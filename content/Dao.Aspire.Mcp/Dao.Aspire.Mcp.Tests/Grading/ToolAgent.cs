using Azure.AI.OpenAI;
using Azure.Identity;
using Dao.Aspire.Mcp.Shared.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.ClientModel;

namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// The "Agent Under Test" (AUT) — an IChatClient pipeline with tool-calling capability.
/// Receives a natural language query, autonomously selects and invokes tools via
/// UseFunctionInvocation(), and returns a ToolAgentResponse with both the synthesized
/// text and a trace of all tool invocations for deterministic assertions.
/// </summary>
public class ToolAgent
{
    private readonly IChatClient _client;

    public ToolAgent(IOptions<AzureOpenAIOptions> options)
    {
        var opts = options.Value;

        var endpoint = new Uri(opts.Endpoint);
        IChatClient innerClient = (opts.ApiKey is not null
                ? new AzureOpenAIClient(endpoint, new ApiKeyCredential(opts.ApiKey))
                : new AzureOpenAIClient(endpoint, new DefaultAzureCredential()))
            .GetChatClient(opts.DeploymentName)
            .AsIChatClient();

        _client = new ChatClientBuilder(innerClient)
            .UseFunctionInvocation()
            .Build();
    }

    /// <summary>
    /// Constructor accepting a pre-configured IChatClient (for unit testing).
    /// </summary>
    public ToolAgent(IChatClient chatClient)
    {
        _client = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation()
            .Build();
    }

    /// <summary>
    /// Sends a natural language query to the agent with the given tools available.
    /// The agent autonomously decides which tools to call and synthesizes a response.
    /// Returns both the response text and a trace of all tool invocations.
    /// </summary>
    public async Task<ToolAgentResponse> QueryAsync(
        string query,
        IList<AIFunction> tools,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, query)
        };

        var chatOptions = new ChatOptions
        {
            Tools = tools.Cast<AITool>().ToList()
        };

        var response = await _client.GetResponseAsync(
            messages,
            chatOptions,
            cancellationToken);

        var invocations = response.Messages
            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
            .Select(fc => new ToolInvocation
            {
                ToolName = fc.Name,
                Arguments = fc.Arguments
            })
            .ToArray();

        return new ToolAgentResponse
        {
            Text = response.Text ?? string.Empty,
            Invocations = invocations
        };
    }
}
