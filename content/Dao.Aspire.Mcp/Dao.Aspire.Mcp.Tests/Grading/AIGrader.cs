namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// Static assertion entry point for AI-graded agentic test validation.
/// Use <c>AIGrader.AssertAsync()</c> in tests the same way you'd use <c>Assert.True()</c>,
/// but for semantically evaluating non-deterministic agent responses against natural language intent.
///
/// Performs dual validation:
/// 1. Deterministic: Verifies expected tools were called (if ExpectedToolNames is set)
/// 2. Semantic: AI grader evaluates response quality against expected behavior
///
/// Only use for agent responses that are inherently non-deterministic.
/// For deterministic behavior (exceptions, exact values), use standard assertions.
/// </summary>
public static class AIGrader
{
    /// <summary>
    /// Evaluates the Agent Under Test's response against the test case's expected behavior.
    /// First performs deterministic assertions on tool invocations (if ExpectedToolNames is set),
    /// then sends the response to the AI grader for semantic evaluation.
    /// </summary>
    /// <param name="grader">The grader agent instance.</param>
    /// <param name="testCase">Test case with query, expected behavior, and essential states.</param>
    /// <param name="agentResponse">The agent's response including text and tool invocation trace.</param>
    /// <param name="minimumConfidence">Minimum grader confidence to accept (default 0.7).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task AssertAsync(
        IGraderAgent grader,
        AgenticTestCase testCase,
        ToolAgentResponse agentResponse,
        double minimumConfidence = 0.7,
        CancellationToken cancellationToken = default)
    {
        // Deterministic assertion: verify expected tools were called
        if (testCase.ExpectedToolNames is { Length: > 0 })
        {
            var actualToolNames = agentResponse.Invocations
                .Select(i => i.ToolName)
                .Distinct()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var expectedTool in testCase.ExpectedToolNames)
            {
                Assert.True(
                    actualToolNames.Contains(expectedTool),
                    $"Expected tool '{expectedTool}' was not called by the agent. " +
                    $"Tools called: [{string.Join(", ", actualToolNames)}]");
            }
        }

        // Semantic assertion: AI grader evaluates response quality
        var result = await grader.GradeAsync(testCase, agentResponse, cancellationToken);

        Assert.NotNull(result);

        Assert.True(
            result.Passed,
            $"""
            AI Grader FAILED for query: '{testCase.Query}'
            Confidence: {result.Confidence:P0}
            Coverage:   {result.Coverage:P0}
            Reasoning:  {result.Reasoning}
            Met:        [{string.Join("; ", result.MetCriteria)}]
            Unmet:      [{string.Join("; ", result.UnmetCriteria)}]
            Tools called: [{string.Join(", ", agentResponse.Invocations.Select(i => i.ToolName))}]
            """);

        Assert.True(
            result.Confidence >= minimumConfidence,
            $"AI Grader confidence too low ({result.Confidence:P0} < {minimumConfidence:P0}). Reasoning: {result.Reasoning}");
    }
}
