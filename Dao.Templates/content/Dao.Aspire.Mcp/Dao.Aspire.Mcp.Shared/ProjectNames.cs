namespace Dao.Aspire.Mcp.Shared;

public static class ProjectNames
{
    public const string McpServer = "mcp-server";
    public const string McpClient = "mcp-client";
    public const string Database = "db";

    extension(Uri)
    {
        public static Uri CreateServiceDiscoveryUri(string projectName, bool isHttps = true)
        {
            return new Uri($"{(isHttps ? "https" : "http")}://{projectName}");
        }
    }
}
