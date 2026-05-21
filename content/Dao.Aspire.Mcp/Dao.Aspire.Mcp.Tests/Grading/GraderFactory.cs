using Dao.Aspire.Mcp.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// Factory for creating GraderAgent and ToolAgent instances using the IOptions pattern.
/// Binds configuration from environment variables, user secrets, or the config file,
/// with DataAnnotation validation matching the project's options conventions.
/// </summary>
public static class GraderFactory
{
    private static IConfigurationRoot BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets("feead503-6b3e-4428-883e-3c575ed3b81e", true)
            .Build();

    /// <summary>
    /// Creates a GraderAgent configured via IOptions&lt;AzureOpenAIOptions&gt;.
    /// </summary>
    public static GraderAgent Create()
    {
        var configuration = BuildConfiguration();

        var services = new ServiceCollection();

        services
            .AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection("Grader"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureOpenAIOptions>>();

        return new GraderAgent(options);
    }

    /// <summary>
    /// Creates a ToolAgent (Agent Under Test) configured via IOptions&lt;AzureOpenAIOptions&gt;.
    /// Uses the "ToolAgent" configuration section.
    /// </summary>
    public static ToolAgent CreateToolAgent()
    {
        var configuration = BuildConfiguration();

        var services = new ServiceCollection();

        services
            .AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection("ToolAgent"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureOpenAIOptions>>();

        return new ToolAgent(options);
    }
}
