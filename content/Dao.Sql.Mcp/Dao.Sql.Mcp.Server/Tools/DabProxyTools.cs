using Dao.Sql.Mcp.Server.Services;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dao.Sql.Mcp.Server.Tools;

/// <summary>
/// Proxy tools that forward requests to Data API Builder (DAB) MCP Server with AI-enhanced query parameters.
/// Implements all six DML tools from DAB: describe_entities, read_records, create_record, update_record, delete_record, execute_entity.
/// </summary>
[McpServerToolType]
public class DabProxyTools(
    DabMcpClientService dabClient,
    IChatClient chatClient,
    IHttpContextAccessor httpContext,
    ILogger<DabProxyTools> logger
)
{
    // Tool name constants
    private const string DescribeEntitiesToolName = "describe_entities";
    private const string ReadRecordsToolName = "read_records";
    private const string CreateRecordToolName = "create_record";
    private const string UpdateRecordToolName = "update_record";
    private const string DeleteRecordToolName = "delete_record";
    private const string ExecuteEntityToolName = "execute_entity";

    private string? GetJwtToken()
    {
        var authHeader = httpContext.HttpContext?.Request.Headers["Authorization"].ToString();
        return authHeader?.Replace("Bearer ", "");
    }

    /// <summary>
    /// Returns entities available to the current role with fields, types, keys, and allowed operations.
    /// Direct pass-through to DAB - no enhancement needed.
    /// </summary>
    [McpServerTool(Name = DescribeEntitiesToolName)]
    [Description(
        "List all available database entities (tables, views, stored procedures) with their fields, types, primary keys, and allowed operations for the current role"
    )]
    public async Task<object> DescribeEntities(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Proxying describe_entities to DAB");

        var jwtToken = GetJwtToken();
        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var response = await dabMcpClient.CallToolAsync(
            DescribeEntitiesToolName,
            new Dictionary<string, object?>(),
            cancellationToken: cancellationToken
        );

        return response.Content;
    }

    /// <summary>
    /// Queries a table or view with AI-enhanced parameters for optimal paging, ordering, and count visibility.
    /// This tool solves the pagination problem where AI agents don't realize there are more results beyond the first page.
    /// </summary>
    [McpServerTool(Name = ReadRecordsToolName)]
    [Description(
        "Query database table or view with filtering, sorting, pagination, and field selection. Returns data with total count and pagination metadata to help AI agents understand when there are more results available."
    )]
    public async Task<EnhancedReadResponse> ReadRecords(
        [Description("Entity name from describe_entities (e.g., 'Product', 'Order')")]
            string entity_name,
        [Description("OData filter expression (e.g., 'Price gt 100')")] string? filter = null,
        [Description("OData orderby expression (e.g., 'Name asc')")] string? orderby = null,
        [Description("Maximum number of records to return (hint: 100 is reasonable default)")]
            int? top = null,
        [Description("Number of records to skip for pagination")] int? skip = null,
        [Description("Comma-separated field names to select")] string? select = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "Proxying read_records for entity {Entity} with AI enhancement",
            entity_name
        );

        var enhancementPrompt =
            $@"Enhance this database query with best practices for paging and ordering:
Entity: {entity_name}
Filter: {filter ?? "none"}
OrderBy: {orderby ?? "none"}
Top: {top?.ToString() ?? "not specified"}
Skip: {skip?.ToString() ?? "not specified"}

Provide recommendations:
1. If 'top' is not specified, suggest a reasonable default (typically 100)
2. If 'orderby' is not specified, suggest ordering by a primary key or common field
3. Always ensure queries can be paginated effectively

Respond in JSON format with: {{{{ ""recommended_top"": number, ""recommended_orderby"": ""string or null"", ""rationale"": ""brief explanation"" }}}}";

        try
        {
            var aiResponse = await chatClient.GetResponseAsync(
                [new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, enhancementPrompt)],
                cancellationToken: cancellationToken
            );
            var responseText = string.Join(
                " ",
                aiResponse
                    .Messages.FirstOrDefault()
                    ?.Contents.OfType<TextContent>()
                    .Select(c => c.Text)
                ?? []
            );
            var aiSuggestion = JsonSerializer.Deserialize<QueryEnhancement>(responseText);

            // Apply AI suggestions if parameters not explicitly set
            top ??= aiSuggestion?.recommended_top ?? 100;
            orderby ??= aiSuggestion?.recommended_orderby;

            logger.LogDebug(
                "AI enhancement: top={Top}, orderby={OrderBy}, rationale={Rationale}",
                top,
                orderby,
                aiSuggestion?.rationale
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI enhancement failed, using defaults");
            top ??= 100;
        }

        // Call DAB with enhanced parameters
        var jwtToken = GetJwtToken();
        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var dabParams = new Dictionary<string, object?>
        {
            ["entity_name"] = entity_name,
            ["filter"] = filter,
            ["orderby"] = orderby,
            ["top"] = top,
            ["skip"] = skip,
            ["select"] = select,
        };

        var dabResponse = await dabMcpClient.CallToolAsync(
            ReadRecordsToolName,
            dabParams,
            cancellationToken: cancellationToken
        );

        // Parse DAB response and add pagination metadata
        var dabData = JsonSerializer.Deserialize<JsonNode>(dabResponse.Content.ToString() ?? "{}");
        var records = dabData?["records"]?.AsArray() ?? new JsonArray();
        var totalCount = dabData?["@odata.count"]?.GetValue<int>() ?? records.Count;

        var returnedCount = records.Count;
        var currentSkip = skip ?? 0;
        var hasMorePages = currentSkip + returnedCount < totalCount;
        var nextSkip = hasMorePages ? currentSkip + returnedCount : (int?)null;

        var enhancedResponse = new EnhancedReadResponse
        {
            entity_name = entity_name,
            records = dabData?["records"] ?? new JsonArray(),
            total_count = totalCount,
            returned_count = returnedCount,
            current_skip = currentSkip,
            current_top = top ?? 100,
            has_more_pages = hasMorePages,
            next_skip = nextSkip,
            filter_applied = filter,
            orderby_applied = orderby,
        };

        logger.LogInformation(
            "read_records returned {ReturnedCount} of {TotalCount} records, has_more_pages={HasMore}",
            returnedCount,
            totalCount,
            hasMorePages
        );

        return enhancedResponse;
    }

    /// <summary>
    /// Creates a new row in a table. Direct pass-through to DAB.
    /// </summary>
    [McpServerTool(Name = CreateRecordToolName)]
    [Description("Insert a new record into a database table")]
    public async Task<object> CreateRecord(
        [Description("Entity name (table)")] string entity_name,
        [Description("Record data as JSON object with field names and values")]
            JsonNode record_data,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying create_record for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var response = await dabMcpClient.CallToolAsync(
            CreateRecordToolName,
            new Dictionary<string, object?>
            {
                ["entity_name"] = entity_name,
                ["record_data"] = record_data,
            },
            cancellationToken: cancellationToken
        );

        return response.Content;
    }

    /// <summary>
    /// Modifies an existing row using its primary key. Direct pass-through to DAB.
    /// </summary>
    [McpServerTool(Name = UpdateRecordToolName)]
    [Description("Update an existing record in a database table using its primary key")]
    public async Task<object> UpdateRecord(
        [Description("Entity name (table)")] string entity_name,
        [Description("Primary key value(s)")] JsonNode key,
        [Description("Fields to update with new values")] JsonNode record_data,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying update_record for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var response = await dabMcpClient.CallToolAsync(
            UpdateRecordToolName,
            new Dictionary<string, object?>
            {
                ["entity_name"] = entity_name,
                ["key"] = key,
                ["record_data"] = record_data,
            },
            cancellationToken: cancellationToken
        );

        return response.Content;
    }

    /// <summary>
    /// Removes a row using its primary key. Direct pass-through to DAB.
    /// </summary>
    [McpServerTool(Name = DeleteRecordToolName)]
    [Description("Delete a record from a database table using its primary key")]
    public async Task<object> DeleteRecord(
        [Description("Entity name (table)")] string entity_name,
        [Description("Primary key value(s)")] JsonNode key,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying delete_record for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var response = await dabMcpClient.CallToolAsync(
            DeleteRecordToolName,
            new Dictionary<string, object?> { ["entity_name"] = entity_name, ["key"] = key },
            cancellationToken: cancellationToken
        );

        return response.Content;
    }

    /// <summary>
    /// Runs a stored procedure with parameters. Direct pass-through to DAB.
    /// </summary>
    [McpServerTool(Name = ExecuteEntityToolName)]
    [Description("Execute a stored procedure with input parameters")]
    public async Task<object> ExecuteEntity(
        [Description("Entity name (stored procedure)")] string entity_name,
        [Description("Input parameters as JSON object")] JsonNode? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying execute_entity for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var response = await dabMcpClient.CallToolAsync(
            ExecuteEntityToolName,
            new Dictionary<string, object?>
            {
                ["entity_name"] = entity_name,
                ["parameters"] = parameters,
            },
            cancellationToken: cancellationToken
        );

        return response.Content;
    }
}

// DTOs for query enhancement and responses
internal record QueryEnhancement(
    int? recommended_top,
    string? recommended_orderby,
    string? rationale
);

public record EnhancedReadResponse
{
    public string entity_name { get; init; } = string.Empty;
    public JsonNode? records { get; init; }
    public int total_count { get; init; }
    public int returned_count { get; init; }
    public int current_skip { get; init; }
    public int current_top { get; init; }
    public bool has_more_pages { get; init; }
    public int? next_skip { get; init; }
    public string? filter_applied { get; init; }
    public string? orderby_applied { get; init; }
}
