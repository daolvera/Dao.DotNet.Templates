var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Dao_Aspire_Expo_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Metro bundler runs only in local dev (publish deploys just the API to the cloud)
if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp("mobile", "../Dao.Aspire.Expo.Mobile")
        .WithRunScript("start")
        .WithReference(api)
        .WithEnvironment("EXPO_PUBLIC_API_URL", api.GetEndpoint("http"));
}

builder.Build().Run();
