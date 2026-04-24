var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Dao_Aspire_Angular_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var frontend = builder.AddJavaScriptApp("frontend", "../Dao.Aspire.Angular.Web")
    .WithRunScript("start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(targetPort: 4200, env: "PORT");

api.PublishWithContainerFiles(frontend, "wwwroot");

builder.Build().Run();
