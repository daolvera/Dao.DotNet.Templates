using Dao.Aspire.Mcp.Server.Tools;
using Dao.Aspire.Mcp.Tests.Fixtures;
using Dao.Aspire.Mcp.Tests.Grading;
using Microsoft.Extensions.AI;

namespace Dao.Aspire.Mcp.Tests.AgenticTests;

/// <summary>
/// True agentic tests for AuthenticatedTool.
/// An AI agent receives natural language queries about user identity and MCP tools,
/// autonomously decides to call the user analysis tool, then a grader evaluates.
/// Each test configures different auth contexts (roles, claims, unauthenticated).
/// </summary>
[Trait("Category", "Agentic")]
public class AuthenticatedToolAgenticTests
{
    private readonly IGraderAgent _grader = GraderFactory.Create();
    private readonly ToolAgent _toolAgent = GraderFactory.CreateToolAgent();

    private static IList<AIFunction> CreateAuthTools(AuthenticatedTool instance) =>
    [
        AIFunctionFactory.Create(
            typeof(AuthenticatedTool).GetMethod(nameof(AuthenticatedTool.AnalyzeMCPServerUser))!,
            instance),
    ];

    [Fact]
    public async Task Agent_CanIdentifyAuthenticatedUser()
    {
        var httpContextAccessor = MockServiceFixtures.CreateMockHttpContextAccessor(
            userId: "user-123",
            userName: "Daniel Olvera",
            roles: ["Admin", "DataAnalyst"]);
        var tool = new AuthenticatedTool(httpContextAccessor);

        var testCase = new AgenticTestCase
        {
            Query = "Who am I logged in as? What roles do I have?",
            ExpectedToolNames = ["AnalyzeMCPServerUser"],
            ExpectedBehavior = "The agent should use the user analysis tool and report the authenticated user's identity, roles, and that the server is operational.",
            EssentialStates =
            [
                "The response indicates the server is operational",
                "The response shows the user's name or identity (Daniel Olvera)",
                "The response includes the user's roles (Admin, DataAnalyst)",
                "A timestamp or check time is mentioned"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateAuthTools(tool));

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanDetectUnauthenticatedState()
    {
        var httpContextAccessor = MockServiceFixtures.CreateUnauthenticatedHttpContextAccessor();
        var tool = new AuthenticatedTool(httpContextAccessor);

        var testCase = new AgenticTestCase
        {
            Query = "Check my user status on the MCP server.",
            ExpectedToolNames = ["AnalyzeMCPServerUser"],
            ExpectedBehavior = "The agent should report that no authenticated user was found while confirming the server is operational.",
            EssentialStates =
            [
                "The response indicates the server is operational",
                "The response clearly states no authenticated user was found or the user is not authenticated",
                "The response does not claim a specific user identity"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateAuthTools(tool));

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanReportUserRoles()
    {
        var httpContextAccessor = MockServiceFixtures.CreateMockHttpContextAccessor(
            userId: "role-user",
            userName: "Role Tester",
            roles: ["Admin", "Developer"]);
        var tool = new AuthenticatedTool(httpContextAccessor);

        var testCase = new AgenticTestCase
        {
            Query = "What roles are assigned to the current user?",
            ExpectedToolNames = ["AnalyzeMCPServerUser"],
            ExpectedBehavior = "The agent should report the user's assigned roles (Admin, Developer) along with identity information.",
            EssentialStates =
            [
                "The Admin role is visible in the response",
                "The Developer role is visible in the response",
                "The server is reported as operational"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateAuthTools(tool));

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanShowUserEmailInClaims()
    {
        var httpContextAccessor = MockServiceFixtures.CreateMockHttpContextAccessor(
            userId: "email-user",
            userName: "Data Analyst",
            email: "analyst@company.com",
            roles: ["DataAnalyst"]);
        var tool = new AuthenticatedTool(httpContextAccessor);

        var testCase = new AgenticTestCase
        {
            Query = "Show me my user information including my email address.",
            ExpectedToolNames = ["AnalyzeMCPServerUser"],
            ExpectedBehavior = "The agent should display user information including the email address 'analyst@company.com' from the claims.",
            EssentialStates =
            [
                "The server is operational",
                "The response contains user identity information",
                "The email 'analyst@company.com' appears in the response"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, CreateAuthTools(tool));

        await AIGrader.AssertAsync(_grader, testCase, response);
    }
}
