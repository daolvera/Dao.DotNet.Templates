# Dao.Sql.Mcp - AI-Enhanced Proxy for SQL MCP Server (Data API Builder)

An intelligent .NET Aspire MCP proxy that sits in front of Microsoft's Data API Builder (DAB) SQL MCP Server, enhancing queries with AI-powered optimization for paging, ordering, and count visibility. **Solves the pagination problem where AI agents don't realize there are more results beyond the first page.**

## The Problem This Solves

When AI agents query databases through Data API Builder's `read_records` tool, they often don't understand pagination:
- Agent asks "How many items are in this table?"
- DAB returns 100 results (default page size)
- Agent assumes there are exactly 100 items
- **Reality**: There might be 250, 1000, or more items

The AI agent has no visibility into:
- Total record count
- Whether more pages exist
- How to fetch subsequent pages

## The Solution

This proxy intercepts MCP tool calls to DAB and enhances them with:
- **Total count metadata** - AI agents see the full dataset size
- **Pagination guidance** - Clear indicators of `has_more_pages`, `next_skip` values
- **AI-suggested defaults** - Intelligent `top` and `orderby` recommendations
- **Pass-through authentication** - JWT tokens forwarded securely to DAB

## Architecture

```
AI Agent (Claude, GPT, etc.)
    ↓
Proxy MCP Server (Dao.Sql.Mcp.Server)
    - AI query enhancement via Azure OpenAI
    - Pagination metadata injection
    - JWT pass-through authentication
    ↓
SQL MCP Server / DAB (Data API Builder)
    - 6 DML tools (describe_entities, read_records, etc.)
    - OData query building
    - RBAC enforcement
    ↓
SQL Server Database
```

## Features

### Core Architecture
- **AI-Enhanced Query Proxy**: Uses `IChatClient` to suggest optimal paging and ordering
- **Transparent Pass-Through**: Other tools (create, update, delete) forward directly to DAB
- **Pagination Visibility**: Returns `total_count`, `has_more_pages`, `next_skip` with every query
- **SQL MCP Server (DAB)**: Microsoft's production-ready MCP implementation for SQL databases
- **Azure AD Authentication**: JWT token validation and forwarding

### MCP Tools (Proxied from DAB)
All six DML tools from Microsoft's SQL MCP Server:
1. **`describe_entities`** - Lists available tables/views/procedures (direct pass-through)
2. **`read_records`** - Queries data with **AI-enhanced paging** and count metadata
3. **`create_record`** - Inserts new records (direct pass-through)
4. **`update_record`** - Modifies existing records (direct pass-through)
5. **`delete_record`** - Removes records (direct pass-through)
6. **`execute_entity`** - Runs stored procedures (direct pass-through)

### Infrastructure
- **.NET Aspire Orchestration**: Manages SQL Server, DAB, and proxy containers
- **Service Discovery**: Automatic service-to-service communication
- **OpenTelemetry**: Built-in logging, tracing, and metrics
- **Health Checks**: Comprehensive health monitoring
- **MCP Inspector**: Test both proxy and direct DAB access

## Prerequisites

- .NET 10 SDK or later
- Docker Desktop (for SQL Server and DAB containers)
- Azure AD tenant (for authentication)
- Azure OpenAI account (for query enhancement)
- Optional: SQL Server Management Studio or Azure Data Studio

## Quick Start

### 1. Install the Template

```bash
dotnet new install Dao.Templates
```

### 2. Create a New Project

```bash
dotnet new sql-mcp-proxy -n MyMcpApp
cd MyMcpApp
```

### 3. Configure Authentication and AI

Update `appsettings.json` in `Dao.Sql.Mcp.Server` project:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "Audience": "api://YOUR-CLIENT-ID",
    "Domain": "yourdomain.onmicrosoft.com"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-openai.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiKey": "YOUR-API-KEY"
  }
}
```

Update `Dao.Sql.Mcp.AppHost/dab-config.json` for DAB authentication:

```json
{
  "runtime": {
    "host": {
      "authentication": {
        "jwt": {
          "audience": "api://YOUR-CLIENT-ID",
          "issuer": "https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0"
        }
      }
    }
  }
}
```

### 4. Run the Application

```bash
dotnet run --project Dao.Sql.Mcp.AppHost
```

The Aspire dashboard opens showing:
- **SQL Server** container
- **DAB MCP Server** (SQL MCP Server) on port 5000
- **Proxy MCP Server** on assigned HTTPS port
- **MCP Inspector** connected to both servers

### 5. Test with MCP Inspector

Open MCP Inspector and test the **Proxy MCP (AI-Enhanced)** server:

**Query Products (Enhanced):**
```json
{
  "entity_name": "Product"
}
```

**Response includes pagination metadata:**
```json
{
  "entity_name": "Product",
  "records": [...],
  "total_count": 250,
  "returned_count": 100,
  "current_skip": 0,
  "current_top": 100,
  "has_more_pages": true,
  "next_skip": 100,
  "filter_applied": null,
  "orderby_applied": "ProductId asc"
}
```

**AI Agent now knows:**
- There are 250 total products (not just 100)
- More pages exist
- Next query should use `skip: 100` to get items 101-200

## Project Structure

```
MyMcpApp/
├── Dao.Sql.Mcp.AppHost/              # Aspire orchestration
│   ├── AppHost.cs                    # Service configuration
│   ├── dab-config.json               # Data API Builder (DAB) configuration
│   └── Dockerfile.dab                # DAB container definition
├── Dao.Sql.Mcp.Server/               # AI-Enhanced MCP Proxy Server
│   ├── Program.cs                    # Authentication and service registration
│   ├── Services/
│   │   └── DabMcpClientService.cs    # DAB MCP client factory
│   └── Tools/
│       └── DabProxyTools.cs          # 6 proxied DML tools with AI enhancement
├── Dao.Sql.Mcp.ServiceDefaults/      # Aspire service defaults (telemetry, health)
├── Dao.Sql.Mcp.Shared/               # Common models and configuration
│   ├── ProjectNames.cs               # Service discovery constants
│   ├── Roles.cs                      # Role constants for authorization
│   └── Options/
│       ├── AzureAdOptions.cs         # Azure AD configuration
│       └── AzureOpenAIOptions.cs     # Azure OpenAI configuration
└── Dao.Sql.Mcp.Tests/                # Integration tests
```

## How AI Query Enhancement Works

When an AI agent calls `read_records`, the proxy:

1. **Receives the query** from the AI agent with entity name and optional filters
2. **Consults IChatClient (Azure OpenAI)** to suggest:
   - Default `top` value (typically 100 if not specified)
   - Appropriate `orderby` clause for consistent pagination
   - Rationale for the suggestions
3. **Forwards enhanced query to DAB** with optimized parameters
4. **Receives DAB response** with raw data and OData count
5. **Augments response** with pagination metadata:
   - `total_count` - Full dataset size
   - `returned_count` - Records in this response
   - `has_more_pages` - Boolean indicator
   - `next_skip` - Value for fetching next page
6. **Returns to AI agent** with complete visibility

This ensures AI agents can:
- Understand the full scope of data available
- Make informed decisions about fetching more pages
- Provide accurate counts to users
- Iterate through large datasets efficiently

## Documentation

### Essential Guides

**[SQL Server MCP Best Practices](docs/SQL_SERVER_MCP_BEST_PRACTICES.md)**  
Complete guide covering:
- Architecture overview and decision matrix
- When to use DAB vs custom tools
- Security best practices (SQL injection prevention, RBAC, RLS)
- Custom query development patterns
- Testing strategies
- Monitoring and observability

**[DAB Configuration Guide](docs/DAB_CONFIGURATION_GUIDE.md)**  
Detailed configuration reference:
- Entity configuration and relationships
- Row-level security (RLS) policies
- Field-level security
- Role-based permissions
- Stored procedure exposure
- Production deployment checklist

### External Resources

- [Data API Builder MCP Overview](https://learn.microsoft.com/en-us/azure/data-api-builder/mcp/overview)
- [Azure SQL Blazor Sample](https://github.com/Azure-Samples/azure-sql-library-app-blazor)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io)

## Security Features

### Azure AD Group-Based RBAC

Define policies in `Program.cs`:

```csharp
options.AddPolicy("DataAnalyst", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireClaim("groups", "12345678-..."); // Azure AD group ID
});
```

Apply to custom tools:

```csharp
[McpServerTool]
[Authorize(Policy = "DataAnalyst")]
public async Task<SalesAnalytics> GetSalesAnalyticsByRegion(...)
```

### Row-Level Security (DAB)

Configure in `dab-config.json`:

```json
{
  "permissions": [{
    "role": "authenticated",
    "actions": [{
      "action": "read",
      "policy": {
        "database": "@item.UserId eq @claims.sub"
      }
    }]
  }]
}
```

Users automatically see only their own records.

### SQL Injection Prevention

All queries are parameterized by default:

```csharp
// EF Core LINQ - automatically parameterized
var orders = await _context.Orders
    .Where(o => o.UserId == userId && o.Status == status)
    .ToListAsync();

// DAB - query builder automatically parameterizes all filters
```

Input validation enforces whitelists:

```csharp
private static readonly HashSet<string> ValidRegions = ["NORTH", "SOUTH", "EAST", "WEST"];
```

## Development Workflow

### Customizing Query Enhancement

The proxy's AI enhancement logic can be customized in `Dao.Sql.Mcp.Server/Tools/DabProxyTools.cs`:

```csharp
// Modify the enhancement prompt in read_records method
var enhancementPrompt = $@"Enhance this database query with best practices:
Entity: {entity_name}
Filter: {filter ?? "none"}

Your custom instructions here...
Respond in JSON: {{ ""recommended_top"": number, ""recommended_orderby"": ""string"" }}";
```

You can adjust:
- Default `top` values based on entity type
- Ordering strategies (temporal data vs. alphabetical)
- Filter optimization suggestions
- Prompt engineering for better AI responses

### Adding Database Entities (DAB Configuration)

To expose new tables/views through the proxy:

1. Ensure table exists in SQL Server database
2. Add entity configuration to `Dao.Sql.Mcp.AppHost/dab-config.json`:

```json
{
  "entities": {
    "NewEntity": {
      "source": {
        "type": "table",
        "object": "dbo.NewTable"
      },
      "description": "Clear description for AI agents to understand the data",
      "permissions": [
        { 
          "role": "authenticated", 
          "actions": [{ "action": "read" }] 
        },
        { 
          "role": "admin", 
          "actions": ["*"] 
        }
      ],
      "mcp": {
        "dml-tools": true
      }
    }
  }
}
```

3. Restart the AppHost - DAB automatically exposes the new entity
4. Test with `describe_entities` to verify it appears

### Testing

```bash
# Run all tests
dotnet test

# Test specific custom tool
dotnet test --filter "FullyQualifiedName~SqlAnalyticsToolsTests"

# Integration tests with TestContainers
dotnet test --filter "Category=Integration"
```

## Azure Deployment

### Prerequisites
- Azure subscription
- Azure CLI or Azure Developer CLI (azd)
- Container registry (ACR) or GitHub Container Registry

### Using Azure Developer CLI

```bash
# Initialize (first time only)
azd init

# Provision and deploy
azd up

# Update DAB configuration with production values
# - Update dab-config.json with Key Vault references
# - Configure managed identity for SQL Server
# - Update Azure AD issuer/audience

# Redeploy after configuration
azd deploy
```

### Production Configuration Checklist

Before going to production, review:

- [ ] Replace placeholder Azure AD credentials in `dab-config.json`
- [ ] Create Application Insights resource in Azure
- [ ] Set `APPLICATIONINSIGHTS_CONNECTION_STRING` in configuration for both DAB and custom server  
- [ ] Store connection strings in Azure Key Vault
- [ ] Enable SQL Server encryption: `Encrypt=True`
- [ ] Configure Azure AD group IDs in authorization policies
- [ ] Enable SQL Server audit logging
- [ ] Set up Application Insights alerts for errors, performance, and availability
- [ ] Configure CORS for production origins (no wildcards)
- [ ] Test all authorization policies with real Azure AD groups
- [ ] Review and test row-level security policies
- [ ] Add rate limiting on endpoints
- [ ] Configure database backups
- [ ] Set up monitoring dashboards
- [ ] Set log levels to Information or higher (not Debug/Trace)

See [SQL_SERVER_MCP_BEST_PRACTICES.md](docs/SQL_SERVER_MCP_BEST_PRACTICES.md) for complete checklist.

## Troubleshooting

### Common Issues

**SQL Server connection fails:**
- Ensure Docker Desktop is running
- Check Aspire dashboard shows SQL Server container as healthy
- Verify connection string is injected correctly

**DAB container fails to start:**
- Validate `dab-config.json` syntax: `dab validate --config dab-config.json`
- Check DAB container logs in Aspire dashboard
- Ensure connection string environment variable is set

**Authorization fails (403 Forbidden):**
- Verify Azure AD token contains required `groups` or `roles` claim
- Check group Object ID matches policy configuration
- Test with MCP Inspector to isolate issue

**Custom tool not discovered:**
- Ensure class has `[McpServerToolType]` attribute
- Verify method has `[McpServerTool]` attribute
- Rebuild solution and restart AppHost

### Debug Mode

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "Dao.Sql.Mcp.Server": "Debug"
    }
  }
}
```

## Performance Optimization

**DAB MCP Built-in Features:**
- **Automatic Caching**: DAB caches `read_records` results (configurable TTL)
- **Connection Pooling**: Managed by DAB's SQL Server integration
- **Query Optimization**: DAB builds deterministic, optimized T-SQL

**Proxy Enhancements:**
- **AI Suggestion Caching**: Consider caching AI enhancement suggestions for common entity types
- **Pagination Defaults**: Proxy applies reasonable defaults (top=100) to prevent unbounded queries
- **Telemetry**: OpenTelemetry spans track proxy overhead vs. DAB call time

**DAB Configuration:**
- Enable caching in `dab-config.json`: `"cache": { "enabled": true, "ttl-seconds": 300 }`
- Add database indexes for frequently filtered/sorted fields
- Use views for complex joins to simplify client queries

## External Resources

- [Microsoft SQL MCP Server Documentation](https://learn.microsoft.com/en-us/azure/data-api-builder/mcp/overview)
- [Data API Builder Documentation](https://learn.microsoft.com/en-us/azure/data-api-builder/)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)

## License

This template is provided as-is for use in your projects.

## Contributing

Issues and pull requests welcome! This is a community-driven template.

## Next Steps

1. Configure Azure AD and Azure OpenAI credentials
2. Customize `dab-config.json` for your database entities
3. Deploy to Azure Container Apps or Azure App Service
4. Test with AI agents (Claude, GPT, etc.) via MCP protocol
5. Monitor query patterns and optimize pagination defaults
6. Add custom database views for complex analytical queries

Happy building with AI-enhanced SQL MCP! 🚀
