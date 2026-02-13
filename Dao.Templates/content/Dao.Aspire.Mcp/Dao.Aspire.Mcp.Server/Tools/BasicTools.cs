using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dao.Aspire.Mcp.Server.Tools;

[McpServerToolType]
public class BasicTools
{
    [McpServerTool, Description("Simple check to confirm if the MCP server is operational.")]
    public static string AnalyzeMCPServerHealth()
    {
        try
        {
            // Get current local time
            DateTime now = DateTime.Now;

            // Format with a custom format (e.g., "2025-10-22 11:00:00")
            string customFormat = now.ToString("yyyy-MM-dd HH:mm:ss");

            return "MCP server is operational. Checked: " + customFormat;
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP request errors
            throw new McpException($"Error performing health check. {ex.Message}", ex);
        }
    }

    [McpServerTool, Description("Echoes the input back to the client.")]
    public static string Echo([Description("The message to echo back")] string message)
    {
        return "Hello from MCP Server. Your input: " + message;
    }

    [McpServerTool, Description("Gives progress updates on the server doing a long computation.")]
    public static async Task<string> LongOperation(
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("How long each step is in seconds")]
        int duration = 2,
        [Description("How many steps the server is given")]
        int steps = 3
        )
    {
        var progressToken = context.Params?.ProgressToken;
        for (int i = 1; i <= steps; i++)
        {
            // Simulate work by delaying for the specified duration
            Thread.Sleep(duration * 1000);
            // Report progress back to the client
            if (progressToken != null)
            {
                await server.SendNotificationAsync("notifications/progress", new
                {
                    Progress = i,
                    Total = steps,
                    progressToken
                });
            }
        }
        return $"Long operation completed in {steps} steps.";
    }
}