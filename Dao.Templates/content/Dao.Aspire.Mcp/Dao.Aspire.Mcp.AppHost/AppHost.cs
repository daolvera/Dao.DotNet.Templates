var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Dao_Aspire_Mcp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Dao_Aspire_Mcp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
