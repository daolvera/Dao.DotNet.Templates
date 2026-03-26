using Dao.Aspire.Ef.Core;
using Dao.Aspire.Ef.Infrastructure.Data;
using Dao.Aspire.Ef.MigrationService;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddAzureNpgsqlDbContext<AppDbContext>(connectionName: ProjectNames.Database, configureDbContextOptions: options =>
{
    string currentAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? throw new InvalidOperationException("Unable to determine the current assembly name.");
    options.UseNpgsql(x => x.MigrationsAssembly(currentAssemblyName));
    options.UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        if (context is AppDbContext dbContext &&
            !await dbContext.Todos.AnyAsync(cancellationToken))
        {
            dbContext.Todos.Add(new Todo
            {
                Name = "Initial Todo",
                DueDate = DateTime.UtcNow.AddDays(1),
                Description = "Test todo, please ignore."
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    });
});

var host = builder.Build();
host.Run();
