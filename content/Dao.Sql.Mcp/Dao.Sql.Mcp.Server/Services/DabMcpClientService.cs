using Dao.Sql.Mcp.Shared;
using ModelContextProtocol.Client;

namespace Dao.Sql.Mcp.Server.Services;

/// <summary>
/// Service for connecting to the Data API Builder (DAB) MCP Server.
/// Creates MCP clients that forward requests to DAB with JWT authentication pass-through.
/// </summary>
public class DabMcpClientService(
    IHttpClientFactory httpClientFactory,
    ILogger<DabMcpClientService> logger
)
{
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
}
