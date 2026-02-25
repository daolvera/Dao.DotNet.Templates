namespace Dao.Sql.Mcp.Shared;

public static class ProjectNames
{
    public const string McpServer = "mcp-server";
    public const string DabMcpServer = "dab-mcp";
    public const string Database = "db";
    public const string DbInit = "db-init";

    extension(Uri)
    {
        public static Uri CreateServiceDiscoveryUri(
            string projectName,
            string path = "/",
            bool isHttps = true
        )
        {
            return new Uri($"{(isHttps ? "https" : "http")}://{projectName}{path}");
        }
    }
}
