using Dao.Aspire.Mcp.Server.Tools;
using Dao.Aspire.Mcp.Tests.Grading;
using Microsoft.Extensions.AI;

namespace Dao.Aspire.Mcp.Tests.AgenticTests;

/// <summary>
/// True agentic tests for BasicTools (Echo, AnalyzeMCPServerHealth).
/// An AI agent receives natural language queries and MCP tools,
/// autonomously decides which tools to call, then a grader evaluates the response.
/// LongOperation is excluded (requires McpServer/RequestContext — integration test concern).
/// </summary>
[Trait("Category", "Agentic")]
public class BasicToolsAgenticTests
{
    private readonly IGraderAgent _grader = GraderFactory.Create();
    private readonly ToolAgent _toolAgent = GraderFactory.CreateToolAgent();

    private static IList<AIFunction> CreateBasicTools() =>
    [
        AIFunctionFactory.Create(BasicTools.Echo),
        AIFunctionFactory.Create(BasicTools.AnalyzeMCPServerHealth),
    ];

    [Fact]
    public async Task Agent_CanCheckServerHealth()
    {
        var testCase = new AgenticTestCase
        {
            Query = "Is the MCP server running? What time was it checked?",
            ExpectedToolNames = ["AnalyzeMCPServerHealth"],
            ExpectedBehavior = "The agent should use the health check tool and report that the server is operational with a timestamp.",
            EssentialStates =
            [
                "The response confirms the server is operational or healthy",
                "A timestamp or date/time is mentioned in the response",
                "No error is reported"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateBasicTools());

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanEchoMessage()
    {
        var testCase = new AgenticTestCase
        {
            Query = "Please echo the message 'Hello, World!' back to me using the echo tool.",
            ExpectedToolNames = ["Echo"],
            ExpectedBehavior = "The agent should use the echo tool with the message 'Hello, World!' and relay the echoed response.",
            EssentialStates =
            [
                "The response contains the original message 'Hello, World!'",
                "The response indicates it came from the MCP Server",
                "The response is not an error"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateBasicTools());

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanEchoSpecialCharacters()
    {
        var testCase = new AgenticTestCase
        {
            Query = "Echo this exact text back to me: Hello <script>alert('xss')</script> & \"quotes\"",
            ExpectedToolNames = ["Echo"],
            ExpectedBehavior = "The agent should echo the special characters (HTML tags, quotes, ampersands) without sanitizing or removing them.",
            EssentialStates =
            [
                "The response contains the special characters from the input",
                "HTML tags or script content was not stripped or sanitized",
                "The response is from the MCP Server"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateBasicTools());

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_SelectsCorrectToolForHealthVsEcho()
    {
        var testCase = new AgenticTestCase
        {
            Query = "Check if the server is healthy and operational.",
            ExpectedToolNames = ["AnalyzeMCPServerHealth"],
            ExpectedBehavior = "The agent should choose the health check tool (not echo) since the user is asking about server status, not echoing a message.",
            EssentialStates =
            [
                "The health check tool was used, not the echo tool",
                "The response indicates the server's operational status",
                "A timestamp is present"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateBasicTools());

        await AIGrader.AssertAsync(_grader, testCase, response);
    }
}
