using Dao.Aspire.Mcp.Shared;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase(ProjectNames.Database);

var mcpServer = builder.AddProject<Projects.Dao_Aspire_Mcp_Server>(ProjectNames.McpServer)
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.Dao_Aspire_Mcp_Client>(ProjectNames.McpClient)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(mcpServer)
    .WaitFor(mcpServer);

builder.Build().Run();
