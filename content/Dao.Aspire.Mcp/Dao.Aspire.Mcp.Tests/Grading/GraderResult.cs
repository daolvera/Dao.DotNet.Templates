namespace Dao.Aspire.Mcp.Tests.Grading;

/// <summary>
/// Structured result from the AI grader agent's evaluation of an MCP tool output.
/// Maps to the blog's concept of coverage metrics and explainability—
/// when validation fails, the result identifies exactly which essential behavior was missed.
/// </summary>
public record GraderResult
{
    public required bool Passed { get; init; }

    /// <summary>
    /// Grader's confidence in its assessment (0.0 = no confidence, 1.0 = certain).
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// Human-readable explanation of why the grader decided pass or fail.
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// Percentage of expected behavior aspects that were met (0.0 - 1.0).
    /// Analogous to the blog's "coverage metric" over essential states.
    /// </summary>
    public required double Coverage { get; init; }

    /// <summary>
    /// Which aspects of the expected behavior were satisfied.
    /// </summary>
    public required string[] MetCriteria { get; init; }

    /// <summary>
    /// Which aspects of the expected behavior were NOT satisfied.
    /// Analogous to the blog's failure reasoning identifying missing essential states.
    /// </summary>
    public required string[] UnmetCriteria { get; init; }
}
