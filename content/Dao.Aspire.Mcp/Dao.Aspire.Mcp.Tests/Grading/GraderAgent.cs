using Azure.AI.OpenAI;
using Azure.Identity;
using Dao.Aspire.Mcp.Shared.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.ClientModel;
using System.Text.Json;

namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// AI grader agent that evaluates MCP tool outputs against natural language expected behavior.
/// This is the "Trust Layer" implementation—an independent LLM that judges correctness
/// semantically rather than through brittle exact-match assertions.
/// 
/// Uses temperature 0 for deterministic grading and structured JSON output.
/// https://github.blog/ai-and-ml/generative-ai/validating-agentic-behavior-when-correct-isnt-deterministic/
/// </summary>
public class GraderAgent : IGraderAgent
{
    private readonly IChatClient _chatClient;

    private static readonly string SystemPrompt = """
        You are an independent AI grader evaluating how well an AI agent answered a user's question.
        The agent had access to MCP (Model Context Protocol) tools and was expected to use them
        to gather information and synthesize an accurate response.

        Your role is the "Trust Layer"—you validate correctness based on INTENT and ESSENTIAL OUTCOMES,
        not on exact string matching or rigid formatting.

        You will receive:
        1. The user's original query
        2. The agent's synthesized response
        3. Which tools the agent called (with arguments)
        4. A natural language description of the expected behavior
        5. A list of "essential states"—aspects that MUST be true for the response to be considered correct

        Your job:
        - Evaluate whether the agent's response satisfies the expected behavior SEMANTICALLY
        - Check each essential state individually
        - Consider whether the agent used tools appropriately (called relevant tools, used reasonable arguments)
        - Be tolerant of wording variations, formatting differences, and incidental details
        - Be strict about essential states—if an essential state is not met, the test fails
        - Focus on WHAT the response communicates, not HOW it's formatted

        Rules for scoring:
        - "passed" = true ONLY if ALL essential states are met
        - "confidence" = your certainty in the assessment (1.0 = absolutely sure)
        - "coverage" = (number of met essential states) / (total essential states)
        - "metCriteria" = list of essential states that were satisfied
        - "unmetCriteria" = list of essential states that were NOT satisfied
        - "reasoning" = concise explanation of WHY you assessed pass/fail
        """;

    public GraderAgent(IOptions<AzureOpenAIOptions> options)
    {
        var opts = options.Value;

        var endpoint = new Uri(opts.Endpoint);
        IChatClient innerClient = (opts.ApiKey is not null
                ? new AzureOpenAIClient(endpoint, new ApiKeyCredential(opts.ApiKey))
                : new AzureOpenAIClient(endpoint, new DefaultAzureCredential()))
            .GetChatClient(opts.DeploymentName)
            .AsIChatClient();

        _chatClient = innerClient;
    }

    /// <summary>
    /// Constructor accepting a pre-configured IChatClient (for unit-testing the grader itself).
    /// </summary>
    public GraderAgent(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<GraderResult?> GradeAsync(
        AgenticTestCase testCase,
        ToolAgentResponse agentResponse,
        CancellationToken cancellationToken = default)
    {
        var userPrompt = BuildUserPrompt(testCase, agentResponse);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var chatOptions = new ChatOptions { Temperature = 0f };

        var graderResultChatResponse = await _chatClient.GetResponseAsync<GraderResult>(
            messages,
            chatOptions,
            useJsonSchemaResponseFormat: true,
            cancellationToken);
        return graderResultChatResponse.TryGetResult(out GraderResult? result) ?
            result : null;

    }

    private static string BuildUserPrompt(AgenticTestCase testCase, ToolAgentResponse agentResponse)
    {
        var essentialStates = string.Join("\n", testCase.EssentialStates.Select((s, i) => $"  {i + 1}. {s}"));

        var toolCalls = agentResponse.Invocations.Length > 0
            ? string.Join("\n", agentResponse.Invocations.Select(inv =>
            {
                var args = inv.Arguments is not null
                    ? JsonSerializer.Serialize(inv.Arguments, new JsonSerializerOptions { WriteIndented = true })
                    : "{}";
                return $"  - {inv.ToolName}({args})";
            }))
            : "  (No tools were called)";

        return $"""
            ## User Query
            {testCase.Query}

            ## Tools Called by Agent
            {toolCalls}

            ## Agent Response
            {agentResponse.Text}

            ## Expected Behavior
            {testCase.ExpectedBehavior}

            ## Essential States (ALL must be true for pass)
            {essentialStates}
            """;
    }
}
