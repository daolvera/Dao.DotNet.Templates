using Dao.Aspire.Mcp.Shared;

var builder = DistributedApplication.CreateBuilder(args);

#if IncludeDatabase
// SQL Server database for application data
var sqlServer = builder
    .AddSqlServer("sql-server")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = sqlServer.AddDatabase(ProjectNames.Database);
#endif

#if IncludeApplicationInsights
var insights = builder.AddAzureApplicationInsights("MyApplicationInsights");
#endif

// Custom MCP Server for complex business logic and analytics tools
var mcpServer = builder
    .AddProject<Projects.Dao_Aspire_Mcp_Server>(ProjectNames.McpServer)
#if IncludeDatabase
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);
#else
    .WithExternalHttpEndpoints();
#endif

#if IncludeMcpInspector
// MCP Inspector for testing custom tools
builder.AddMcpInspector("inspector").WithMcpServer(mcpServer).WaitFor(mcpServer);
#endif

#if IncludeWebClient
builder
    .AddProject<Projects.Dao_Aspire_Mcp_Client>(ProjectNames.McpClient)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(mcpServer)
    .WaitFor(mcpServer);
#endif

builder.Build().Run();
