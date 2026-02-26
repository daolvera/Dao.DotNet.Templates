using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dao.Sql.Mcp.Server.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Server;

namespace Dao.Sql.Mcp.Server.Tools;

/// <summary>
/// Proxy tools that forward requests to Data API Builder (DAB) MCP Server with AI-enhanced query parameters.
/// Implements all six DML tools from DAB: describe_entities, read_records, create_record, update_record, delete_record, execute_entity.
/// Write tools (create, update, delete, execute) are gated by dynamic DAB tool discovery — if a tool is
/// disabled in dab-config.json it will not be forwarded and a clear error is returned instead.
/// </summary>
[McpServerToolType]
public class DabProxyTools(
    DabMcpClientService dabClient,
    IChatClient chatClient,
    IHttpContextAccessor httpContext,
    IMemoryCache memoryCache,
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
    /// Ensures the specified tool is enabled in DAB before forwarding. Throws McpException if not enabled.
    /// Returns immediately (allowing the call) when the DAB tool list cannot be fetched.
    /// </summary>
    private async Task EnsureToolEnabledAsync(
        string toolName,
        string? jwtToken,
        CancellationToken cancellationToken
    )
    {
        var enabledTools = await dabClient.GetEnabledToolsAsync(jwtToken, cancellationToken);

        // Empty set means discovery failed — fail open rather than blocking all writes
        if (enabledTools.Count == 0)
            return;

        if (!enabledTools.Contains(toolName))
            throw new InvalidOperationException(
                $"Tool '{toolName}' is not enabled in the DAB configuration. "
                    + "Contact your administrator to enable it in dab-config.json."
            );
    }

    /// <summary>
    /// Extracts the text payload from a DAB MCP CallToolResult Content list.
    /// Serializes the content array and extracts the first "text" property value.
    /// </summary>
    private static string ExtractDabResponseText(object content)
    {
        try
        {
            var json = JsonSerializer.Serialize(content);
            var array = JsonSerializer.Deserialize<JsonArray>(json);
            // Content items have a "text" property for text-type entries
            return array?.FirstOrDefault()?["text"]?.GetValue<string>() ?? json;
        }
        catch
        {
            return content?.ToString() ?? "{}";
        }
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
    /// Queries a table or view with AI-enhanced parameters for optimal paging, ordering, projections, and join expansion.
    /// This tool solves the pagination problem where AI agents don't realize there are more results beyond the first page.
    /// Use <paramref name="expand"/> to fetch related entities in a single call (avoids N+1 queries).
    /// Use <paramref name="select"/> to request only the fields you need (improves performance on wide tables).
    /// </summary>
    [McpServerTool(Name = ReadRecordsToolName)]
    [Description(
        "Query a database table or view with filtering, sorting, pagination, field projection, and relationship expansion. "
            + "Returns data with total count and pagination metadata so AI agents know when more results are available. "
            + "Use 'select' to project only needed fields (e.g., 'Id,Name,Price'). "
            + "Use 'expand' to include related entities in one call instead of making separate queries (e.g., 'OrderItems,User' when querying Orders)."
    )]
    public async Task<EnhancedReadResponse> ReadRecords(
        [Description("Entity name from describe_entities (e.g., 'Product', 'Order')")]
            string entity_name,
        [Description("OData filter expression (e.g., 'Price gt 100 and IsAvailable eq true')")]
            string? filter = null,
        [Description("OData orderby expression (e.g., 'Name asc, CreatedAt desc')")]
            string? orderby = null,
        [Description("Maximum number of records to return (default: 100)")] int? top = null,
        [Description(
            "Number of records to skip for pagination (e.g., 100 for second page when top=100)"
        )]
            int? skip = null,
        [Description(
            "Comma-separated field names to project — omit unneeded fields to reduce payload (e.g., 'Id,Name,Price')"
        )]
            string? select = null,
        [Description(
            "Comma-separated related entity names to expand in a single call, avoiding N+1 queries (e.g., 'OrderItems,User' when querying Orders)"
        )]
            string? expand = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "Proxying read_records for entity {Entity} with AI enhancement",
            entity_name
        );

        // Fetch entity field names to give AI schema context (cached per entity)
        var entityFields = await GetEntityFieldsAsync(
            entity_name,
            GetJwtToken(),
            cancellationToken
        );
        var fieldsHint =
            entityFields.Count > 0
                ? $"Available fields: {string.Join(", ", entityFields)}"
                : "Fields: unknown (use describe_entities to list them)";

        var enhancementPrompt =
            $@"You are optimizing a database query. Apply these best practices:
Entity: {entity_name}
{fieldsHint}
Filter: {filter ?? "none"}
OrderBy: {orderby ?? "not specified — recommend one"}
Top: {top?.ToString() ?? "not specified — recommend a safe default"}
Skip: {skip?.ToString() ?? "0"}
Select: {select ?? "not specified — recommend key fields only"}
Expand: {expand ?? "not specified — recommend related entities if the entity has known relationships"}

Rules:
1. If 'top' is missing, recommend 100 as a safe default.
2. If 'orderby' is missing, recommend ordering by the primary key or a natural sort field from the available fields.
3. If 'select' is missing, recommend the most useful subset of fields (avoid selecting all fields on wide tables).
4. If related entities are likely useful (e.g., querying Orders should expand OrderItems), recommend them in 'recommended_expand'.
5. Keep recommendations concise and valid for OData syntax.

Output in JSON format matching QueryEnhancement structure with fields: recommended_top, recommended_orderby, recommended_select, recommended_expand, rationale.";

        string? aiOrderBy = null;
        string? aiSelect = null;

        try
        {
            var chatOptions = new ChatOptions { ResponseFormat = ChatResponseFormat.Json };

            var aiResponse = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, enhancementPrompt)],
                chatOptions,
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

            // Apply AI suggestions only when the caller did not specify a value
            top ??= aiSuggestion?.recommended_top ?? 100;
            aiOrderBy = aiSuggestion?.recommended_orderby;
            orderby ??= aiOrderBy;
            aiSelect = aiSuggestion?.recommended_select;
            select ??= aiSelect;
            expand ??= aiSuggestion?.recommended_expand;

            logger.LogDebug(
                "AI enhancement: top={Top}, orderby={OrderBy}, select={Select}, expand={Expand}, rationale={Rationale}",
                top,
                orderby,
                select,
                expand,
                aiSuggestion?.rationale
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI enhancement failed, using defaults");
            top ??= 100;
        }

        // Call DAB with enhanced parameters; request OData count for accurate pagination metadata
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
            ["expand"] = expand,
            ["$count"] = true, // Request total count from DAB for accurate pagination metadata
        };

        var dabResponse = await dabMcpClient.CallToolAsync(
            ReadRecordsToolName,
            dabParams,
            cancellationToken: cancellationToken
        );

        // Parse DAB response and add pagination metadata
        var responseText2 = ExtractDabResponseText(dabResponse.Content);
        var dabData = JsonSerializer.Deserialize<JsonNode>(
            responseText2.Length > 0 ? responseText2 : "{}"
        );
        var records = dabData?["records"]?.AsArray() ?? new JsonArray();

        // @odata.count is present when $count=true is requested; fall back to page count if absent
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
            select_applied = select,
            expand_applied = expand,
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
    /// Creates a new row in a table. Requires DAB to have create_record enabled in dab-config.json.
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
        await EnsureToolEnabledAsync(CreateRecordToolName, jwtToken, cancellationToken);

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
    /// Modifies an existing row using its primary key. Requires DAB to have update_record enabled in dab-config.json.
    /// </summary>
    [McpServerTool(Name = UpdateRecordToolName)]
    [Description(
        "Update an existing record in a database table using its primary key. "
            + "Requires update_record to be enabled in the DAB configuration (dab-config.json)."
    )]
    public async Task<object> UpdateRecord(
        [Description("Entity name (table)")] string entity_name,
        [Description("Primary key value(s)")] JsonNode key,
        [Description("Fields to update with new values")] JsonNode record_data,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying update_record for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        await EnsureToolEnabledAsync(UpdateRecordToolName, jwtToken, cancellationToken);

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
    /// Removes a row using its primary key. Requires DAB to have delete_record enabled in dab-config.json.
    /// </summary>
    [McpServerTool(Name = DeleteRecordToolName)]
    [Description(
        "Delete a record from a database table using its primary key. "
            + "Requires delete_record to be enabled in the DAB configuration (dab-config.json)."
    )]
    public async Task<object> DeleteRecord(
        [Description("Entity name (table)")] string entity_name,
        [Description("Primary key value(s)")] JsonNode key,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying delete_record for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        await EnsureToolEnabledAsync(DeleteRecordToolName, jwtToken, cancellationToken);

        var dabMcpClient = await dabClient.CreateClientAsync(jwtToken);

        var response = await dabMcpClient.CallToolAsync(
            DeleteRecordToolName,
            new Dictionary<string, object?> { ["entity_name"] = entity_name, ["key"] = key },
            cancellationToken: cancellationToken
        );

        return response.Content;
    }

    /// <summary>
    /// Runs a stored procedure with parameters. Requires DAB to have execute_entity enabled in dab-config.json.
    /// </summary>
    [McpServerTool(Name = ExecuteEntityToolName)]
    [Description(
        "Execute a stored procedure with input parameters. "
            + "Requires execute_entity to be enabled in the DAB configuration (dab-config.json)."
    )]
    public async Task<object> ExecuteEntity(
        [Description("Entity name (stored procedure)")] string entity_name,
        [Description("Input parameters as JSON object")] JsonNode? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Proxying execute_entity for entity {Entity}", entity_name);

        var jwtToken = GetJwtToken();
        await EnsureToolEnabledAsync(ExecuteEntityToolName, jwtToken, cancellationToken);

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

    /// <summary>
    /// Fetches field names for a given entity from DAB's describe_entities response, cached per entity name.
    /// Returns an empty list on failure so that enhancement can proceed without schema context.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetEntityFieldsAsync(
        string entityName,
        string? jwtToken,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = $"dab:entity_fields:{entityName}";
        if (memoryCache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        try
        {
            var client = await dabClient.CreateClientAsync(jwtToken);
            var result = await client.CallToolAsync(
                DescribeEntitiesToolName,
                new Dictionary<string, object?>(),
                cancellationToken: cancellationToken
            );

            var text = ExtractDabResponseText(result.Content);
            var root = JsonSerializer.Deserialize<JsonNode>(text);

            // DAB describe_entities returns an array of entity descriptors with a "fields" array
            var entities = root?.AsArray() ?? root?["entities"]?.AsArray();
            var entity = entities?.FirstOrDefault(e =>
                string.Equals(
                    e?["name"]?.GetValue<string>(),
                    entityName,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            var fields =
                entity
                    ?["fields"]?.AsArray()
                    .Select(f => f?["name"]?.GetValue<string>() ?? f?.GetValue<string>() ?? "")
                    .Where(n => n.Length > 0)
                    .ToList() ?? [];

            memoryCache.Set(cacheKey, (IReadOnlyList<string>)fields, TimeSpan.FromMinutes(10));
            return fields;
        }
        catch (Exception ex)
        {
            logger.LogDebug(
                ex,
                "Could not fetch entity fields for {Entity}; proceeding without schema context",
                entityName
            );
            return [];
        }
    }
}

// DTOs for query enhancement and responses
internal record QueryEnhancement(
    int? recommended_top,
    string? recommended_orderby,
    string? recommended_select,
    string? recommended_expand,
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
    public string? select_applied { get; init; }
    public string? expand_applied { get; init; }
}
