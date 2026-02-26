using Dao.Sql.Mcp.Shared;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Client;

namespace Dao.Sql.Mcp.Server.Services;

/// <summary>
/// Service for connecting to the Data API Builder (DAB) MCP Server.
/// Creates MCP clients that forward requests to DAB with JWT authentication pass-through.
/// </summary>
public class DabMcpClientService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    ILogger<DabMcpClientService> logger
)
{
    private const string EnabledToolsCacheKey = "dab:enabled_tools";

    /// <summary>
    /// Creates an MCP client connected to the DAB MCP Server.
    /// </summary>
    /// <param name="jwtToken">JWT bearer token to forward to DAB for authentication</param>
    /// <param name="mcpPath">MCP endpoint path (default: /mcp)</param>
    /// <returns>Configured MCP client</returns>
    public async Task<McpClient> CreateClientAsync(string? jwtToken, string mcpPath = "/mcp")
    {
        logger.LogDebug("Creating DAB MCP client connection to {Path}", mcpPath);

        var httpClient = httpClientFactory.CreateClient(ProjectNames.DabMcpServer);

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = Uri.CreateServiceDiscoveryUri(ProjectNames.DabMcpServer, mcpPath),
            AdditionalHeaders = string.IsNullOrWhiteSpace(jwtToken) ?
                null :
                new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {jwtToken}" },
                },
        };

        var transport = new HttpClientTransport(transportOptions, httpClient);

        var client = await McpClient.CreateAsync(transport);

        logger.LogInformation("DAB MCP client connected successfully");

        return client;
    }

    /// <summary>
    /// Returns the set of tool names currently enabled in DAB, cached for 5 minutes.
    /// Returns an empty set on failure (fail-open — tool availability checked at DAB level).
    /// </summary>
    public async Task<IReadOnlySet<string>> GetEnabledToolsAsync(
        string? jwtToken,
        CancellationToken cancellationToken = default
    )
    {
        if (
            memoryCache.TryGetValue(EnabledToolsCacheKey, out IReadOnlySet<string>? cached)
            && cached != null
        )
            return cached;

        try
        {
            var client = await CreateClientAsync(jwtToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            IReadOnlySet<string> names = tools.Select(t => t.Name).ToHashSet();

            memoryCache.Set(EnabledToolsCacheKey, names, TimeSpan.FromMinutes(5));
            logger.LogInformation(
                "Discovered {Count} enabled DAB tools: {Names}",
                names.Count,
                string.Join(", ", names)
            );
            return names;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not discover DAB tool list; allowing tool call through");
            return new HashSet<string>();
        }
    }
}
