using Dao.Sql.Mcp.Data;
using Dao.Sql.Mcp.DbInit;
using Dao.Sql.Mcp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<AppDbContext>(ProjectNames.Database);

builder.Services.AddHostedService<DatabaseInitializer>();

var app = builder.Build();
await app.RunAsync();