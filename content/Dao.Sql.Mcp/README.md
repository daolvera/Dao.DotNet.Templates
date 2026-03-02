# Dao.Sql.Mcp - Data API Builder (DAB) MCP Server Template

A .NET Aspire template for deploying Microsoft's Data API Builder (DAB) as an MCP server, providing AI agents with secure, authenticated access to SQL Server databases through standardized MCP tools.

## What This Template Provides

This template gives you a complete .NET Aspire solution for running Data API Builder (DAB) as an MCP server:
- **Pre-configured DAB MCP Server** - Microsoft's production-ready SQL MCP implementation
- **Azure AD Authentication** - JWT token validation with role-based access control
- **SQL Server Integration** - Local development with Docker, production-ready connection handling
- **.NET Aspire Orchestration** - Automatic service discovery, health checks, and observability
- **MCP Inspector Support** - Test and debug MCP tools during development

## Architecture

```
AI Agent (Claude, GPT, etc.)
    ↓
DAB MCP Server (Data API Builder)
    - 6 DML tools (describe_entities, read_records, etc.)
    - OData query building
    - JWT authentication
    - RBAC enforcement
    ↓
SQL Server Database
```

## Features

### Core Capabilities
- **DAB MCP Server**: Microsoft's production-ready MCP implementation for SQL databases
- **6 MCP Tools**: Full CRUD operations plus stored procedure execution
- **OData Query Support**: Powerful filtering, sorting, and pagination
- **Azure AD Authentication**: JWT token validation with role-based access control

### MCP Tools (from DAB)
All six DML tools from Microsoft's SQL MCP Server:

1. **`describe_entities`** - Lists available tables/views/procedures with schema information
2. **`read_records`** - Queries data with OData filters, ordering, and pagination
3. **`create_record`** - Inserts new records
4. **`update_record`** - Modifies existing records
5. **`delete_record`** - Removes records
6. **`execute_entity`** - Runs stored procedures

### Infrastructure
- **.NET Aspire Orchestration**: Manages SQL Server and DAB containers
- **Service Discovery**: Automatic service-to-service communication
- **OpenTelemetry**: Built-in logging, tracing, and metrics
- **Health Checks**: Comprehensive health monitoring
- **MCP Inspector**: Test DAB MCP tools during development

## Prerequisites

- .NET 10 SDK or later
- Docker Desktop (for SQL Server and DAB containers)
- Azure AD tenant (for authentication)
- Optional: SQL Server Management Studio or Azure Data Studio

## Quick Start

### 1. Install the Template

```bash
dotnet new install Dao.Templates
```

### 2. Create a New Project

```bash
dotnet new sql-mcp -n MyMcpApp
cd MyMcpApp
```

### 3. Configure Authentication

Update `appsettings.json` in `Dao.Sql.Mcp.Server` project (if using Azure AD):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR-TENANT-ID",
    "ClientId": "YOUR-CLIENT-ID",
    "Audience": "api://YOUR-CLIENT-ID",
    "Domain": "yourdomain.onmicrosoft.com"
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
- **MCP Inspector** connected to DAB

### 5. Test with MCP Inspector

Open MCP Inspector and test the DAB MCP server:

**Query Products:**
```json
{
  "entity_name": "Product",
  "$top": 10,
  "$orderby": "ProductId"
}
```

**Response:**
```json
{
  "records": [...],
  "metadata": {
    "count": 10
  }
}
```

## Project Structure

```
MyMcpApp/
├── Dao.Sql.Mcp.AppHost/              # Aspire orchestration
│   ├── AppHost.cs                    # Service configuration
│   ├── dab-config.json               # Data API Builder (DAB) configuration
│   └── Dockerfile.dab                # DAB container definition
├── Dao.Sql.Mcp.DbInit/               # Database initialization
│   └── DatabaseInitializer.cs        # Sample data seeding
├── Dao.Sql.Mcp.ServiceDefaults/      # Aspire service defaults (telemetry, health)
├── Dao.Sql.Mcp.Shared/               # Common models and configuration
│   ├── ProjectNames.cs               # Service discovery constants
│   ├── Roles.cs                      # Role constants for authorization
│   └── Options/
│       └── AzureAdOptions.cs         # Azure AD configuration
└── Dao.Sql.Mcp.Tests/                # Integration tests
```

## How DAB MCP Works

## How DAB MCP Works

When an AI agent calls MCP tools through DAB:

1. **Agent sends MCP request** - Uses tools like `read_records` with entity name and optional filters
2. **DAB validates JWT token** - Ensures user is authenticated and authorized
3. **DAB builds OData query** - Converts MCP parameters to optimized T-SQL
4. **Query executes on SQL Server** - With appropriate filtering, paging, and ordering
5. **DAB returns structured data** - JSON response with records and metadata
6. **Agent processes results** - Uses data to answer user questions or take actions

DAB handles:
- **Authentication & Authorization**: JWT validation and role-based access control
- **Query Building**: OData to T-SQL conversion with parameterization
- **Security**: SQL injection prevention, row-level security enforcement
- **Performance**: Query optimization, connection pooling, optional caching

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

### Customizing DAB Configuration

You can customize DAB's behavior in `Dao.Sql.Mcp.AppHost/dab-config.json`:

```json
{
  "runtime": {
    "cache": {
      "enabled": true,
      "ttl-seconds": 5
    },
    "pagination": {
      "default-page-size": 100,
      "max-page-size": 100000
    }
  }
}
```

You can adjust:
- Default and maximum page sizes
- Caching behavior
- CORS policies
- Authentication settings

### Adding Database Entities (DAB Configuration)

To expose new tables/views through DAB:

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

- [ ] Configure Azure AD credentials in `dab-config.json`
- [ ] Create Application Insights resource in Azure
- [ ] Set `APPLICATIONINSIGHTS_CONNECTION_STRING` in DAB configuration
- [ ] Store connection strings in Azure Key Vault
- [ ] Enable SQL Server encryption: `Encrypt=True`
- [ ] Configure Azure AD group IDs in DAB permissions
- [ ] Enable SQL Server audit logging
- [ ] Set up Application Insights alerts for errors, performance, and availability
- [ ] Configure CORS for production origins (no wildcards)
- [ ] Test all authorization policies with real Azure AD groups
- [ ] Review and test row-level security policies
- [ ] Add rate limiting if using API Management
- [ ] Configure database backups
- [ ] Set up monitoring dashboards
- [ ] Set DAB log levels to Information or higher (not Debug/Trace)

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

**DAB Configuration:**

- Enable caching in `dab-config.json`: `"cache": { "enabled": true, "ttl-seconds": 300 }`
- Add database indexes for frequently filtered/sorted fields
- Use views for complex joins to simplify client queries
- Configure appropriate page sizes based on your data

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

1. Configure Azure AD credentials in `dab-config.json`
2. Customize database entities for your schema
3. Deploy to Azure Container Apps or Azure App Service
4. Test with AI agents (Claude, GPT, etc.) via MCP protocol
5. Monitor query patterns and optimize database indexes
6. Add row-level security policies for multi-tenant scenarios
7. Consider Azure API Management for additional API governance

Happy building with DAB MCP! 🚀
