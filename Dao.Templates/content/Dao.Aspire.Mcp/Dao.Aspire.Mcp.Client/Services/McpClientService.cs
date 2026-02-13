using Azure.AI.OpenAI;
using Azure.Identity;
using Dao.Aspire.Mcp.Shared;
using Dao.Aspire.Mcp.Shared.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using System.ClientModel;

namespace Dao.Aspire.Mcp.Client.Services;

public class McpClientService(HttpClient serverClient, IOptions<AzureOpenAIOptions> azureOpenAIOptions)
{
    public async Task<McpClient> CreateClientAsync(string accessToken)
    {
        var endpoint = new Uri(azureOpenAIOptions.Value.Endpoint);
        IChatClient client = (azureOpenAIOptions.Value.ApiKey is not null ?
                new AzureOpenAIClient(endpoint, new ApiKeyCredential(azureOpenAIOptions.Value.ApiKey)) :
                new AzureOpenAIClient(endpoint, new DefaultAzureCredential()))
            .GetChatClient(azureOpenAIOptions.Value.DeploymentName)
            .AsIChatClient();


        var clientTransportOptions = new HttpClientTransportOptions
        {
            Endpoint = Uri.CreateServiceDiscoveryUri(ProjectNames.McpServer),
            // Currently using these options do NOT work for entra ID because of RFC 8707
            // (MCP Standard Issue 1614) [https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1614]
            // (MCP C# SDK PR 617) [https://github.com/modelcontextprotocol/csharp-sdk/pull/617#discussion_r2201965892]
            // OAuth = new()
            // {
            // 	RedirectUri = new(azureAdOptions.Value.RedirectUri
            // 		?? throw new InvalidOperationException("Redirect URI is required for the MCP Client")),
            // 	ClientId = azureAdOptions.Value.ClientId,
            // 	Scopes = azureAdOptions.Value.Scopes,
            // 	ClientSecret = azureAdOptions.Value.ClientSecret,
            // }
            AdditionalHeaders = new Dictionary<string, string>()
            {
                { "Authorization", $"Bearer {accessToken}" }
            }
        };
        var clientTransport = new HttpClientTransport(clientTransportOptions, serverClient);

        // Connect to an MCP server
        return await McpClient.CreateAsync(
            clientTransport,
            clientOptions: new()
            {
                Handlers = new()
                {
                    SamplingHandler = client.CreateSamplingHandler()
                }
            });
    }
}
