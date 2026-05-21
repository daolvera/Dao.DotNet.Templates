var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Dao_Aspire_Avalonia_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddProject<Projects.Dao_Aspire_Avalonia_Desktop>("desktop")
        .WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"))
        .WaitFor(api);
}

builder.Build().Run();
