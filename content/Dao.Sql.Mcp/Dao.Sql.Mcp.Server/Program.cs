using Azure.AI.OpenAI;
using Azure.Identity;
using Dao.Sql.Mcp.Server.Services;
using Dao.Sql.Mcp.Shared;
using Dao.Sql.Mcp.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Configure Azure OpenAI options
builder
    .Services.AddOptions<AzureOpenAIOptions>()
    .BindConfiguration(AzureOpenAIOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register IChatClient for AI query enhancement
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    var endpoint = new Uri(options.Endpoint);
    return (options.ApiKey != null
            ? new AzureOpenAIClient(endpoint, new ApiKeyCredential(options.ApiKey))
            : new AzureOpenAIClient(endpoint, new DefaultAzureCredential()))
        .GetChatClient(options.DeploymentName).AsIChatClient();
});

// Register DAB MCP Client Service
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<DabMcpClientService>();
builder.Services.AddHttpClient(ProjectNames.DabMcpServer);

builder
    .Services.AddOptions<AzureAdOptions>()
    .BindConfiguration(AzureAdOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var azureAdOptions =
    builder.Configuration.GetSection(AzureAdOptions.SectionName).Get<AzureAdOptions>()
    ?? throw new InvalidOperationException("Azure AD options are required");

var authBuilder = builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            AuthorizationServers =
            {
                // Configure the authorization server based on Entra ID (Azure AD) v2.0 endpoint for OAuth 2.1
                new($"{azureAdOptions.Instance}{azureAdOptions.TenantId}/v2.0"),
            },
            ScopesSupported = [.. azureAdOptions.Scopes],
        };
    })
    .AddMicrosoftIdentityWebApi(
        jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = new()
            {
                ValidateAudience = true,
                ValidAudience =
                    azureAdOptions.Audience
                    ?? throw new InvalidOperationException("Audience is required"),
                ValidateIssuer = true,
                ValidIssuer = $"{azureAdOptions.Instance}/{azureAdOptions.TenantId}/v2.0",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5),
            };
        },
        microsoftOptions =>
        {
            microsoftOptions.Instance = azureAdOptions.Instance;
            microsoftOptions.ClientId = azureAdOptions.ClientId;
            microsoftOptions.TenantId = azureAdOptions.TenantId;
            microsoftOptions.Domain = azureAdOptions.Domain;
        }
    );

builder.Services.AddAuthorization(options =>
{
    // the default policy requires authentication via MCP
    options.FallbackPolicy = options.DefaultPolicy;
});

builder
    .Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "MCP Templates", Version = "0.7.0" };
    })
    .AddAuthorizationFilters()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly();

builder.Services.AddHttpContextAccessor();

// you can also add tools, prompts, and resources from
// other assemblies or add them manually as needed with the overloads of WithTools, WithPrompts, and WithResources

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapMcp("mcp").RequireAuthorization();

app.Run();
