using Dao.Aspire.Mcp.Client.Components;
using Dao.Aspire.Mcp.Client.Services;
using Dao.Aspire.Mcp.Shared;
using Dao.Aspire.Mcp.Shared.Options;
#if IncludeAuthentication
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .Services.AddOptions<AzureOpenAIOptions>()
    .BindConfiguration(AzureOpenAIOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

#if IncludeAuthentication
builder
    .Services.AddOptions<AzureAdOptions>()
    .BindConfiguration(AzureAdOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder
    .Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(AzureAdOptions.SectionName))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
#endif

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient<McpClientService>(options =>
{
    options.BaseAddress = Uri.CreateServiceDiscoveryUri(ProjectNames.McpServer);
});

#if IncludeAuthentication
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
#endif

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

#if IncludeAuthentication
app.UseAuthentication();
app.UseAuthorization();
#endif

app.UseAntiforgery();

app.MapStaticAssets();
#if IncludeAuthentication
app.MapControllers();
#endif
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
