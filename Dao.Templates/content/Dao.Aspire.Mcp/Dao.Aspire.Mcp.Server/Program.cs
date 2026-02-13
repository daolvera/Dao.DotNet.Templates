using Dao.Aspire.Mcp.Data;
using Dao.Aspire.Mcp.Shared;
using Dao.Aspire.Mcp.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<AppDbContext>(ProjectNames.Database);

builder.Services.AddOptions<AzureAdOptions>()
    .BindConfiguration(AzureAdOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var azureAdOptions = builder.Configuration
    .GetSection(AzureAdOptions.SectionName)
    .Get<AzureAdOptions>()
    ?? throw new InvalidOperationException("Azure AD options are required");

var authBuilder = builder.Services
    .AddAuthentication(options =>
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
                // Configure the authorization server based on Entra ID (Azure AD)
                new($"{azureAdOptions.Instance}/{azureAdOptions.TenantId}"),
            },
            ScopesSupported = [.. azureAdOptions.Scopes]
        };
    })
    .AddMicrosoftIdentityWebApi(jwtOptions =>
    {
        jwtOptions.TokenValidationParameters = new()
        {
            ValidateAudience = true,
            ValidAudience = azureAdOptions.Audience ?? throw new InvalidOperationException("Audience is required"),
            ValidateIssuer = true,
            ValidIssuer = $"{azureAdOptions.Instance}/{azureAdOptions.TenantId}/v2.0",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    },
    microsoftOptions =>
    {
        microsoftOptions.Instance = azureAdOptions.Instance;
        microsoftOptions.ClientId = azureAdOptions.ClientId;
        microsoftOptions.TenantId = azureAdOptions.TenantId;
        microsoftOptions.Domain = azureAdOptions.Domain;
    });

builder.Services.AddAuthorization(options =>
{
    // the default policy requires authentication via MCP
    options.FallbackPolicy = options.DefaultPolicy;

    options.AddPolicy("CanAccessRestrictedTools", policy =>
    {
        // Add required scopes for accessing restricted tools
        policy.RequireScope("RestrictedTools");
        // you can also do role based
        // policy.RequireRole("Admin");
    });
});

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "MCP Templates",
            Version = "0.7.0"
        };
    })
    .AddAuthorizationFilters()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly();
// you can also add tools, prompts, and resources from
// other assemblies or add them manually as needed with the overloads of WithTools, WithPrompts, and WithResources

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();


app
    .MapMcp()
    .RequireAuthorization();

app.Run();
