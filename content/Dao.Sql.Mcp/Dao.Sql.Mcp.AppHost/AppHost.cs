using Dao.Sql.Mcp.Shared;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// SQL Server database for application data
var sqlServer = builder
    .AddSqlServer("sql-server")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = sqlServer.AddDatabase(ProjectNames.Database);

// Database initialization project - seeds the database using EF Core (idempotent)
var dbInit = builder
    .AddProject<Projects.Dao_Sql_Mcp_DbInit>(ProjectNames.DbInit)
    .WithReference(db)
    .WaitFor(db);

var insights = builder.AddAzureApplicationInsights("MyApplicationInsights");

// SQL MCP Server (Data API Builder) - provides 6 DML tools
// Tools: describe_entities, create_record, read_records, update_record, delete_record, execute_entity
var dabMcpServer = builder
    .AddDockerfile("dab-mcp", ".", "Dockerfile.dab")
    .WithHttpEndpoint(targetPort: 5000, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("MSSQL_CONNECTION_STRING", db)
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", insights)
    .WaitFor(dbInit)
    .WithUrls(x =>
    {
        x.Urls.Add(
            new()
            {
                Url = "/swagger",
                DisplayText = "Swagger",
                Endpoint = x.GetEndpoint("http"),
            }
        );
    });

// AI-Enhanced MCP Proxy Server - sits in front of DAB to add query intelligence
// Enhances read_records with paging metadata and AI-suggested defaults
// Solves pagination visibility problem for AI agents
var mcpServer = builder
    .AddProject<Projects.Dao_Sql_Mcp_Server>(ProjectNames.McpServer)
    .WithExternalHttpEndpoints()
    .WithEnvironment("services__dab-mcp__http__0", dabMcpServer.GetEndpoint("http"))
    .WaitFor(dabMcpServer);

// MCP Inspector for testing proxy access
builder.AddMcpInspector("inspector").WithMcpServer(mcpServer).WaitFor(mcpServer);

builder.Build().Run();
