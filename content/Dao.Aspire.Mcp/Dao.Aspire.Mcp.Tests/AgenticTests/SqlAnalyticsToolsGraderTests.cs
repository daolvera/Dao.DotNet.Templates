using Dao.Aspire.Mcp.Data;
using Dao.Aspire.Mcp.Server.Tools;
using Dao.Aspire.Mcp.Tests.Fixtures;
using Dao.Aspire.Mcp.Tests.Grading;
using Microsoft.Extensions.AI;
using ModelContextProtocol;

namespace Dao.Aspire.Mcp.Tests.AgenticTests;

/// <summary>
/// True agentic tests for SqlAnalyticsTools.
/// An AI agent receives natural language queries about sales/inventory and MCP tools,
/// autonomously decides which tools to call with what parameters, then a grader evaluates.
/// Deterministic behaviors (exceptions, validation errors) use standard assertions.
/// </summary>
[Trait("Category", "Agentic")]
public class SqlAnalyticsToolsAgenticTests : IDisposable
{
    private readonly IGraderAgent _grader = GraderFactory.Create();
    private readonly ToolAgent _toolAgent = GraderFactory.CreateToolAgent();
    private readonly AppDbContext _dbContext;
    private readonly IList<AIFunction> _tools;
    private readonly SqlAnalyticsTools _sqlTools;

    public SqlAnalyticsToolsAgenticTests()
    {
        _dbContext = MockServiceFixtures.CreateSeededDbContext();
        var httpContextAccessor = MockServiceFixtures.CreateMockHttpContextAccessor(
            roles: ["DataAnalyst", "Developers"]);
        var logger = MockServiceFixtures.CreateMockLogger<SqlAnalyticsTools>();
        _sqlTools = new SqlAnalyticsTools(_dbContext, httpContextAccessor, logger);

        _tools = CreateSqlAnalyticsTools(_sqlTools);
    }

    private static IList<AIFunction> CreateSqlAnalyticsTools(SqlAnalyticsTools instance) =>
    [
        AIFunctionFactory.Create(
            typeof(SqlAnalyticsTools).GetMethod(nameof(SqlAnalyticsTools.GetSalesAnalyticsByRegion))!,
            instance),
        AIFunctionFactory.Create(
            typeof(SqlAnalyticsTools).GetMethod(nameof(SqlAnalyticsTools.GetInventoryProjections))!,
            instance),
    ];

    [Fact]
    public async Task Agent_CanQuerySalesAcrossAllRegions()
    {
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var testCase = new AgenticTestCase
        {
            Query = $"Show me the sales analytics across all regions from {startDate} to {endDate}.",
            ExpectedToolNames = ["GetSalesAnalyticsByRegion"],
            ExpectedBehavior = "The agent should query sales analytics for all regions and present aggregated data including total orders, revenue, and regional breakdowns.",
            EssentialStates =
            [
                "The response contains region-level sales data",
                "Revenue figures are mentioned",
                "Order counts are included",
                "The date range is reflected in the response"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, _tools);

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanFilterSalesByRegion()
    {
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var testCase = new AgenticTestCase
        {
            Query = $"What are the sales numbers for only the NORTH region between {startDate} and {endDate}?",
            ExpectedToolNames = ["GetSalesAnalyticsByRegion"],
            ExpectedBehavior = "The agent should query sales analytics filtered to the NORTH region only. The response should not include data from other regions.",
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

    [Fact]
    public async Task Agent_CanAnalyzeInventoryForAllCategories()
    {
        var testCase = new AgenticTestCase
        {
            Query = "Analyze the current inventory levels and give me reorder recommendations for all product categories.",
            ExpectedToolNames = ["GetInventoryProjections"],
            ExpectedBehavior = "The agent should query inventory projections for all categories and present stock levels, sales velocity, and reorder recommendations.",
            EssentialStates =
            [
                "The response includes multiple products",
                "Stock levels or quantities are mentioned",
                "Reorder recommendations or stock status is provided for products",
                "A total inventory value is mentioned"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, _tools);

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    [Fact]
    public async Task Agent_CanFilterInventoryByCategory()
    {
        var testCase = new AgenticTestCase
        {
            Query = "Show me inventory projections only for Electronics products.",
            ExpectedToolNames = ["GetInventoryProjections"],
            ExpectedBehavior = "The agent should query inventory projections filtered to the Electronics category. Non-electronics products should be excluded.",
            EssentialStates =
            [
                "The response focuses on Electronics products",
                "Non-electronics products (like chairs or furniture) are not listed",
                "Stock analysis for electronics items is provided"
            ]
        };

        var response = await _toolAgent.QueryAsync(testCase.Query, _tools);

        await AIGrader.AssertAsync(_grader, testCase, response);
    }

    // --- Deterministic tests (standard assertions, no AI grader) ---

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
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _sqlTools.GetSalesAnalyticsByRegion(startDate, endDate, "INVALID_REGION"));

        Assert.Contains("Invalid region", ex.Message);
    }

    [Fact]
    public async Task GetSalesAnalyticsByRegion_InvalidDateFormat_ThrowsMcpException()
    {
        var ex = await Assert.ThrowsAsync<McpException>(
            () => _sqlTools.GetSalesAnalyticsByRegion("not-a-date", "also-not-a-date"));

        Assert.Contains("Invalid date format", ex.Message);
    }

    [Fact]
    public async Task GetInventoryProjections_InvalidLookbackDays_ThrowsMcpException()
    {
        var ex = await Assert.ThrowsAsync<McpException>(
            () => _sqlTools.GetInventoryProjections(lookbackDays: 0));

        Assert.Contains("Lookback days must be between 1 and 365", ex.Message);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
