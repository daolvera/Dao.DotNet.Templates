namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// Interface for the AI grader agent that evaluates agent responses.
/// Represents the "Trust Layer" from the GitHub blog post—
/// an independent validator that judges correctness semantically.
/// </summary>
public interface IGraderAgent
{
    /// <summary>
    /// Evaluates the Agent Under Test's response against the test case's
    /// expected behavior description and essential states.
    /// </summary>
    /// <param name="testCase">The test case definition with expected behavior.</param>
    /// <param name="agentResponse">The agent's synthesized response and tool invocation trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Structured grading result with pass/fail, confidence, and reasoning.</returns>
    Task<GraderResult?> GradeAsync(
        AgenticTestCase testCase,
        ToolAgentResponse agentResponse,
        CancellationToken cancellationToken = default);
}
