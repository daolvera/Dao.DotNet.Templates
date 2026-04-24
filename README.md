# Dao.Templates

A collection of .NET project templates for accelerating development with modern patterns focused on AI integration.

**Installation:**

```bash
dotnet new install Dao.Templates
```

## Templates

### .NET Aspire MCP Server and Client (`dao-aspire-mcp`)

Full-featured .NET Aspire application with Model Context Protocol server and Blazor client.

## .NET Aspire Sql Server MCP (`dao-sql-mcp`)

.NET Aspire application that hosts a [Data Api Sql Server](https://learn.microsoft.com/en-us/azure/data-api-builder/mcp/overview) to provide access to a database in a governed and managed way.

## .NET Aspire + Angular (`dao-aspire-angular`)

.NET Aspire application with an Angular frontend and C# API, ready for cloud deployment with `azd up`. Uses the backend-serves-frontend model — in dev mode Angular runs separately with a proxy, in production a single container serves both the API and the Angular static files.

## .NET Aspire with EF (`dao-aspire-ef`)

.NET Aspire application that has Entity Framework Core with a migration service created to quickstart apps with the needed infrastructure.

## Documentation

For detailed information about each template, see the README in the respective template directory.

## License

See [LICENSE.txt](LICENSE.txt)
