using Dao.Aspire.Ef.Core;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer(ProjectNames.DatabaseProvider)
    .RunAsContainer();
//options =>
//    options.WithLifetime(ContainerLifetime.Persistent));

var postgresdb = postgres.AddDatabase(ProjectNames.Database);

var migrations = builder.AddProject<Projects.Dao_Aspire_Ef_MigrationService>(ProjectNames.MigrationService)
    .WaitFor(postgresdb)
    .WithReference(postgresdb);

builder.AddDbGate()
    .WaitForCompletion(migrations)
    .WithReference(postgresdb);

builder.AddProject<Projects.Dao_Aspire_Ef_ApiService>(ProjectNames.Api)
    .WaitForCompletion(migrations)
    .WithReference(postgresdb)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
