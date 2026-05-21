namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// Defines a single agentic test case: a natural language query for the Agent Under Test,
/// paired with expected behavior descriptions for grading.
/// The agent autonomously decides which tools to call and how—
/// the test validates the outcome, not the exact path.
/// "EssentialStates" map to the blog's dominator concept—
/// aspects that MUST be present regardless of output variation.
/// </summary>
public record AgenticTestCase
{
    /// <summary>
    /// Natural language query sent to the Agent Under Test.
    /// The agent decides which tools to call based on this prompt.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Optional: expected tool names the agent should call.
    /// When set, AIGrader.AssertAsync deterministically verifies these tools were invoked.
    /// Null means any tool selection is acceptable as long as the response passes grading.
    /// </summary>
    public string[]? ExpectedToolNames { get; init; }

    /// <summary>
    /// Natural language description of what the agent's response should satisfy.
    /// This is the "intent" that the grader evaluates against.
    /// </summary>
    public required string ExpectedBehavior { get; init; }

    /// <summary>
    /// Must-have aspects of the response (dominator nodes in the blog's terminology).
    /// Each entry is a short NL statement that MUST be true for the test to pass.
    /// </summary>
    public required string[] EssentialStates { get; init; }
}
