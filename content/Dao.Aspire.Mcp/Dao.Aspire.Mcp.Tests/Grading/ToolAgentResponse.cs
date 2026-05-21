namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// Response from the Agent Under Test, capturing both the final synthesized text
/// and the trace of tool invocations the agent made autonomously.
/// The invocation trace enables deterministic assertions on tool selection
/// while the text is graded semantically by the AI grader.
/// </summary>
public record ToolAgentResponse
{
    /// <summary>
    /// The agent's final synthesized text response.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Ordered trace of tool invocations the agent made during its reasoning loop.
    /// </summary>
    public required ToolInvocation[] Invocations { get; init; }
}

/// <summary>
/// A single tool invocation captured from the agent's function-calling loop.
/// </summary>
public record ToolInvocation
{
    /// <summary>
    /// Name of the tool/function that was called.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Arguments the agent constructed for the tool call.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; init; }
}
