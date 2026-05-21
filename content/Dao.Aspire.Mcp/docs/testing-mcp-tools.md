# Testing MCP Tools: A Two-Layer Strategy for Deterministic and Agentic Validation

MCP (Model Context Protocol) tools occupy a unique position in modern software architecture. They are functions with deterministic logic—input validation, data queries, business rules—that are ultimately consumed by a non-deterministic caller: an LLM agent. This duality demands a testing strategy that addresses both dimensions.

This document outlines a pragmatic, two-layer approach to MCP tool testing: **traditional unit tests** for the code you control, and **AI-graded agentic tests** for the behavior you can't predict.

> **Reference:** This strategy draws on the concepts from GitHub's blog post [*Validating agentic behavior when "correct" isn't deterministic*](https://github.blog/ai-and-ml/generative-ai/validating-agentic-behavior-when-correct-isnt-deterministic/), which introduces dominator analysis and the "Trust Layer" for validating autonomous agent behavior.

---

## The Problem: Two Kinds of Correctness

When you build an MCP tool like `GetSalesAnalyticsByRegion`, its internal logic is entirely deterministic. Given an invalid date range, it should throw. Given valid inputs and seeded data, it should return the same result every time. Standard unit tests handle this perfectly.

But here's what unit tests **cannot** tell you:

- Will an agent actually **choose** this tool when a user asks *"What were our sales last month?"*
- Will the agent construct **reasonable arguments** (correct date format, valid region name)?
- Will the agent **synthesize** the tool's output into a response that actually answers the user's question?

These behaviors are inherently non-deterministic. The agent might rephrase, reformat, call tools in a different order, or combine multiple tool results. Traditional assertions—exact string matching, fixed output comparisons—become brittle and produce false negatives. The test fails, but the agent succeeded.

This is the gap that GitHub's blog describes: **"The agent didn't fail. The validation did."**

---

## Layer 1: Unit Tests for Deterministic Logic

Unit tests remain the foundation. MCP tools are still code. They have input validation, business rules, exception paths, and data access logic that must be verified with precision.

### What to Test

| Concern | Example | Assertion Style |
|---|---|---|
| Input validation | Invalid date format, out-of-range parameters | `Assert.ThrowsAsync<McpException>` |
| Business rules | Date range ordering, region whitelist | Exact exception message matching |
| Data correctness | Query returns expected records from seeded data | `Assert.Equal`, `Assert.Contains` |
| Edge cases | Empty results, null inputs, boundary values | Standard assertions |
| Authorization | Role-based access, unauthenticated state | Mock claims, verify behavior |

### Example: Deterministic Validation Tests

```csharp
// These tests validate the tool's internal logic directly.
// No agent, no LLM, no grading—just standard unit testing.

[Fact]
public async Task GetSalesAnalyticsByRegion_InvalidDateRange_ThrowsMcpException()
{
    var ex = await Assert.ThrowsAsync<McpException>(
        () => _sqlTools.GetSalesAnalyticsByRegion("2025-12-01", "2025-01-01"));

    Assert.Contains("End date must be after start date", ex.Message);
}

[Fact]
public async Task GetSalesAnalyticsByRegion_InvalidRegion_ThrowsMcpException()
{
    var ex = await Assert.ThrowsAsync<McpException>(
        () => _sqlTools.GetSalesAnalyticsByRegion(startDate, endDate, "INVALID_REGION"));

    Assert.Contains("Invalid region", ex.Message);
}
```

These tests run fast, require no external services, and provide the deterministic guarantees you need. **Do not skip them.** They are the bedrock of confidence that your tool behaves correctly when called with specific inputs.

---

## Layer 2: AI-Graded Agentic Tests for Non-Deterministic Behavior

Once you've validated that your tools are correct in isolation, the next question is: *do they work correctly when an autonomous agent is the one calling them?*

This is where the testing model fundamentally shifts. You are no longer testing a function—you are testing a **conversation**. The "input" is a natural language query, and the "output" is a synthesized response that an LLM produced by autonomously selecting and invoking your tools.

### The Core Insight: Essential States Over Exact Outputs

GitHub's blog introduces the concept of **dominator analysis** from compiler theory: in an execution graph, a "dominator" node is one that *every* valid path must pass through. Applied to agent testing, this means defining the **essential states**—the aspects that must be true for a response to be correct—while tolerating variation in wording, formatting, tool ordering, and incidental details.

A response that says *"The NORTH region generated $1,605 in revenue across 2 orders"* is semantically equivalent to *"Revenue: $1,605.00 | Orders: 2 | Region: NORTH."* A human reviewer would accept both. Your tests should too.

### The Architecture: Agent Under Test + Grader Agent

The agentic testing pattern uses two independent LLM-powered components:

```
                    ┌────────────────────────┐
   User Query ────▶ │   Agent Under Test     │──── Tool Invocations ───▶ MCP Tools
                    │   (ToolAgent)           │◀─── Tool Results ────────┘
                    └──────────┬─────────────┘
                               │
                        Synthesized Response
                        + Invocation Trace
                               │
                               ▼
                    ┌────────────────────────┐
                    │   Grader Agent          │──── Structured Verdict
                    │   (Trust Layer)         │     (pass/fail, confidence,
                    └────────────────────────┘      coverage, reasoning)
```

**Agent Under Test (`ToolAgent`):** Receives a natural language query and a set of MCP tools. It autonomously decides which tools to call, constructs arguments, and synthesizes a response. This is the system you're validating.

**Grader Agent (`GraderAgent`):** An independent LLM configured with temperature 0 for deterministic evaluation. It receives the test case definition (query, expected behavior, essential states), the agent's response, and the tool invocation trace. It returns a structured verdict.

**Key design principle:** The grader is not the same model judging itself. It's an independent "Trust Layer" that evaluates *outcomes* semantically, not a self-assessment. This directly addresses the accuracy gap that GitHub's blog found—where agent self-assessment achieved only 82% accuracy versus the structural approach at 100%.

### Defining Test Cases: The `AgenticTestCase`

Each agentic test is defined as a natural language specification:

```csharp
var testCase = new AgenticTestCase
{
    // What the user asks
    Query = "Show me the sales analytics across all regions for the last 30 days.",

    // Which tools MUST be called (deterministic assertion)
    ExpectedToolNames = ["GetSalesAnalyticsByRegion"],

    // Natural language description of correct behavior
    ExpectedBehavior = "The agent should query sales analytics for all regions " +
                       "and present aggregated data including orders, revenue, " +
                       "and regional breakdowns.",

    // Essential states: the dominator nodes
    EssentialStates =
    [
        "The response contains region-level sales data",
        "Revenue figures are mentioned",
        "Order counts are included",
        "The date range is reflected in the response"
    ]
};
```

Notice the structure:

- **`Query`** is a realistic user prompt, not a contrived test input.
- **`ExpectedToolNames`** provides a deterministic checkpoint—did the agent even call the right tool?
- **`ExpectedBehavior`** gives the grader semantic context for what "correct" looks like.
- **`EssentialStates`** are the dominator nodes—the must-have aspects that every valid response shares, regardless of formatting or phrasing.

### Dual Validation: Deterministic + Semantic

The `AIGrader.AssertAsync` method performs both validation layers in sequence:

1. **Deterministic assertion:** If `ExpectedToolNames` is set, it verifies the agent actually called those tools. This is a hard gate—no LLM judgment needed.

2. **Semantic assertion:** The grader agent evaluates the response against the essential states and returns a structured `GraderResult`:
   - `Passed`: Did all essential states hold?
   - `Confidence`: How certain is the grader? (must exceed a configurable minimum, default 0.7)
   - `Coverage`: Percentage of essential states that were met.
   - `MetCriteria` / `UnmetCriteria`: Exactly which states passed or failed.
   - `Reasoning`: Natural language explanation of the verdict.

```csharp
[Fact]
public async Task Agent_CanFilterSalesByRegion()
{
    var testCase = new AgenticTestCase
    {
        Query = "What are the sales numbers for only the NORTH region?",
        ExpectedToolNames = ["GetSalesAnalyticsByRegion"],
        ExpectedBehavior = "The agent should query sales filtered to NORTH only.",
        EssentialStates =
        [
            "The response focuses on the NORTH region",
            "Other regions (SOUTH, EAST) are not presented as separate data",
            "Revenue and order figures specific to NORTH are shown"
        ]
    };

    var response = await _toolAgent.QueryAsync(testCase.Query, _tools);

    await AIGrader.AssertAsync(_grader, testCase, response);
}
```

When this test fails, the output is diagnostic, not cryptic:

```
AI Grader FAILED for query: 'What are the sales numbers for only the NORTH region?'
Confidence: 90%
Coverage:   67%
Reasoning:  The response included NORTH region data but also listed SOUTH region
            figures in a comparison table, violating the exclusivity requirement.
Met:        [The response focuses on the NORTH region; Revenue and order figures shown]
Unmet:      [Other regions (SOUTH, EAST) are not presented as separate data]
Tools called: [GetSalesAnalyticsByRegion]
```

This is the **explainability** the blog advocates for—when validation fails, you know exactly which essential state was missed and why.

---

## When to Use Which Layer

| Scenario | Layer | Why |
|---|---|---|
| Input validation logic | Unit Test | Deterministic, exact assertions |
| Exception paths and error messages | Unit Test | Deterministic, exact assertions |
| Data transformation correctness | Unit Test | Deterministic with seeded data |
| Tool selection by an agent | Agentic Test | Non-deterministic LLM decision |
| Argument construction by an agent | Agentic Test | Non-deterministic parameter generation |
| Response synthesis quality | Agentic Test | Non-deterministic natural language output |
| Multi-tool orchestration | Agentic Test | Non-deterministic tool sequencing |
| Auth-aware behavior (agent perspective) | Agentic Test | Agent must interpret identity data meaningfully |

The guiding principle: **if the behavior under test has exactly one correct output, use a unit test. If many outputs could be correct, use the grader.**

---

## Practical Considerations

### Test Data and Fixtures

Agentic tests still need controlled environments. Use in-memory databases with seeded data and mock HTTP contexts to ensure the tools return predictable results. The non-determinism is in the *agent's behavior*, not in the underlying data.

```csharp
// Deterministic fixture setup for non-deterministic agent testing
_dbContext = MockServiceFixtures.CreateSeededDbContext();
var httpContextAccessor = MockServiceFixtures.CreateMockHttpContextAccessor(
    roles: ["DataAnalyst", "Developers"]);
```

### Cost and Speed

Agentic tests call LLM APIs twice per test (once for the agent, once for the grader). They are slower and more expensive than unit tests. Categorize them separately:

```csharp
[Trait("Category", "Agentic")]
```

Run unit tests on every commit. Run agentic tests on PR boundaries or scheduled pipelines.

### Confidence Thresholds

The minimum confidence parameter (default 0.7) provides a safety valve. If the grader is uncertain about its own verdict, the test fails. This guards against hallucinated assessments.

### Writing Good Essential States

Essential states should be:

- **Observable:** Based on what the response contains, not internal implementation details.
- **Necessary:** If this state is missing, the response is genuinely wrong—not just formatted differently.
- **Independent:** Each state should be evaluable on its own without depending on other states.
- **Tolerant:** State *"Revenue figures are mentioned"* is better than *"Revenue is exactly $1,605.00"* unless exact values are critical.

---

## The Bigger Picture

MCP tools bridge the gap between deterministic software and non-deterministic AI agents. Testing them requires embracing that duality rather than forcing one paradigm across both concerns.

Unit tests give you fast, cheap, reliable verification that your tool logic is correct. Agentic tests give you confidence that an LLM agent can actually *use* your tools effectively in the context of real user queries.

Together, they form a complete testing strategy: **verify correctness at the code level, then validate behavior at the agent level.** The first catches bugs in your logic. The second catches gaps in your tool design—unclear descriptions, ambiguous parameter names, missing context—that only surface when an autonomous agent tries to use what you built.

> *"We don't need black-box models to judge other black-box models. We need structural guarantees developers can inspect, reason about, and trust."*
> — [GitHub Blog: Validating agentic behavior when "correct" isn't deterministic](https://github.blog/ai-and-ml/generative-ai/validating-agentic-behavior-when-correct-isnt-deterministic/)
