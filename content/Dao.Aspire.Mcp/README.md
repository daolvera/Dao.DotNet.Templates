# .NET Aspire MCP Server with SQL Server

A comprehensive .NET Aspire application template featuring a Model Context Protocol (MCP) server with custom tools for database operations and business logic, all secured with Azure AD and SQL Server.

## Features

### Core Architecture
- **MCP Architecture**: Custom tools for database operations and complex queries
- **SQL Server Integration**: Production-ready SQL Server with EF Core and connection pooling
- **Azure AD RBAC**: Group-based authorization for securing tools and data access
- **SQL Injection Protection**: Parameterized queries and input validation by design

### MCP Tools
- **Custom Tools**: 
  - Example CRUD operations
  - Sales analytics by region with product rankings
  - Inventory projections based on sales velocity
  - Customer lifetime value calculations
  - Extensible architecture for additional business intelligence tools

### Infrastructure
- **Blazor Client**: Interactive web application with Azure AD authentication
- **Service Discovery**: Aspire-managed service references
- **Health Checks**: Comprehensive health monitoring
- **OpenTelemetry**: Built-in logging, tracing, and metrics
- **Container Orchestration**: Aspire manages SQL Server and custom MCP server

## Prerequisites

- .NET 10 SDK or later
- Docker Desktop (for SQL Server container)
- Azure AD tenant (for authentication)
- Optional: SQL Server Management Studio or Azure Data Studio

## Quick Start

### 1. Install the Template

```bash
dotnet new install Dao.Templates
```

### 2. Create a New Project

```bash
dotnet new aspire-mcp -n MyMcpApp
cd MyMcpApp
```

### 3. Configure Azure AD

Update `appsettings.json` in Server and Client projects with your Azure AD credentials:

```json
{
  "AzureAd": {
    "ClientId": "YOUR-CLIENT-ID",
    "TenantId": "YOUR-TENANT-ID",
    "Instance": "https://login.microsoftonline.com/"
  }
}
```

### 4. Run the Application

```bash
dotnet run --project Dao.Aspire.Mcp.AppHost
```

The Aspire dashboard will open showing:
- **SQL Server** container on default port
- **MCP Server** on assigned HTTPS port
- **Blazor Client** on assigned HTTPS port
- **MCP Inspector** for testing tools

### 5. Test MCP Tools

Open the MCP Inspector from the Aspire dashboard and explore:

**Custom Tools:**
- `GetSalesAnalyticsByRegion` - Complex sales analysis with date ranges
- `GetInventoryProjections` - Stock management recommendations
- `GetCustomerLifetimeValue` - CLV calculations and segmentation
- Example CRUD operations with authentication

## Project Structure

```
MyMcpApp/
├── Dao.Aspire.Mcp.AppHost/          # Aspire orchestration
│   └── AppHost.cs                    # Service configuration
├── Dao.Aspire.Mcp.Server/            # MCP Server
│   ├── Program.cs                    # Authorization policies
│   ├── Prompts/                      # MCP prompts
│   ├── Resources/                    # MCP resources
│   └── Tools/
│       ├── SqlAnalyticsTools.cs      # Custom analytical tools
│       ├── AuthenticatedTool.cs      # Example authenticated tool
│       └── BasicTools.cs             # Example unauthenticated tools
├── Dao.Aspire.Mcp.Client/            # Blazor web application
│   └── Services/
│       └── McpClientService.cs       # MCP client integration
├── Dao.Aspire.Mcp.ServiceDefaults/   # Shared configuration
├── Dao.Aspire.Mcp.Shared/            # Common models and options
└── Dao.Aspire.Mcp.Tests/             # Unit and integration tests
```

## Documentation

### Essential Guides

**SQL Server MCP Best Practices**  
Complete guide covering:
- Architecture overview
- Security best practices (SQL injection prevention, RBAC)
- Custom tool development patterns
- Testing strategies
- Monitoring and observability

### External Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io)
- [Azure SQL Best Practices](https://learn.microsoft.com/azure/azure-sql/database/best-practices-overview)

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

### Row-Level Security

Implement data filtering in your tools:

```csharp
[McpServerTool]
[Authorize]
public async Task<List<Order>> GetUserOrders(
    [Description("User ID")] string userId)
{
    var currentUserId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Ensure users can only see their own orders
    return await _context.Orders
        .Where(o => o.UserId == currentUserId)
        .ToListAsync();
}
```

### SQL Injection Prevention

All queries are parameterized by default:

```csharp
// EF Core LINQ - automatically parameterized
var orders = await _context.Orders
    .Where(o => o.UserId == userId && o.Status == status)
    .ToListAsync();
```

Input validation enforces whitelists:

```csharp
private static readonly HashSet<string> ValidRegions = ["NORTH", "SOUTH", "EAST", "WEST"];

if (!ValidRegions.Contains(region.ToUpper()))
    throw new ArgumentException($"Invalid region: {region}");
```

## Development Workflow

### Adding Custom Tools

1. Create a new tool class in `Dao.Aspire.Mcp.Server/Tools/`
2. Decorate with `[McpServerToolType]`
3. Add methods with `[McpServerTool]` and `[Description]`
4. Apply `[Authorize(Policy = "...")]` for RBAC
5. Use EF Core LINQ for parameterized queries
6. Return strongly-typed DTOs

Example:

```csharp
[McpServerToolType]
public class MyBusinessTools(AppDbContext context, ILogger<MyBusinessTools> logger)
{
    [McpServerTool]
    [Description("Calculate quarterly revenue by product category")]
    [Authorize(Policy = "FinancialReports")]
    public async Task<QuarterlyRevenue> GetQuarterlyRevenue(
        [Description("Year (e.g., 2024)")] int year,
        [Description("Quarter (1-4)")] int quarter)
    {
        // Parameterized query logic
        var results = await context.Orders
            .Where(o => o.OrderDate.Year == year && ...)
            .GroupBy(o => o.Product.Category)
            .Select(g => new CategoryRevenue { ... })
            .ToListAsync();
            
        return new QuarterlyRevenue { Categories = results };
    }
}
```

### Adding Database Entities

When adding new tables:

1. Create entity model class
2. Add navigation properties and relationships
3. Configure in DbContext if needed
4. Add migration: `dotnet ef migrations add AddNewEntity`
5. Create corresponding MCP tools for the entity

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

# Update configuration with production values
# - Configure managed identity for SQL Server
# - Update Azure AD issuer/audience

# Redeploy after configuration
azd deploy
```

### Production Configuration Checklist

Before going to production, review:

- [ ] Configure Azure AD credentials in configuration
- [ ] Create Application Insights resource in Azure
- [ ] Set `APPLICATIONINSIGHTS_CONNECTION_STRING` in MCP server configuration  
- [ ] Store connection strings in Azure Key Vault
- [ ] Enable SQL Server encryption: `Encrypt=True`
- [ ] Configure Azure AD group IDs in authorization policies
- [ ] Enable SQL Server audit logging
- [ ] Set up Application Insights alerts for errors, performance, and availability
- [ ] Configure CORS for production origins (no wildcards)
- [ ] Test all authorization policies with real Azure AD groups
- [ ] Add rate limiting on endpoints
- [ ] Configure database backups
- [ ] Set up monitoring dashboards
- [ ] Set log levels to Information or higher (not Debug/Trace)

## Troubleshooting

### Common Issues

**SQL Server connection fails:**

- Ensure Docker Desktop is running
- Check Aspire dashboard shows SQL Server container as healthy
- Verify connection string is injected correctly

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
      "Dao.Aspire.Mcp.Server": "Debug"
    }
  }
}
```

## Performance Optimization

- **Connection Pooling**: Enabled by default via Aspire SQL Server integration
- **Query Optimization**: Use `.AsNoTracking()` for read-only queries
- **Indexing**: Add indexes to frequently filtered/joined columns
- **Pagination**: Enforce `Take()` limits on all list queries
- **Caching**: Use `IDistributedCache` for reference data
- **Async Operations**: Use async methods throughout for better scalability

## License

This template is provided as-is for use in your projects.

## Contributing

Issues and pull requests welcome! This is a community-driven template.

## Next Steps

1. Configure Azure AD credentials
2. Add database entities and migrations
3. Create custom MCP tools for your business logic
4. Implement authorization policies
5. Set up CI/CD pipeline
6. Deploy to Azure

Happy building! 🚀
