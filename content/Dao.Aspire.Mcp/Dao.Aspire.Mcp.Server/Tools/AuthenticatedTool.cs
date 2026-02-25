using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Dao.Aspire.Mcp.Server.Tools;

[McpServerToolType]
[Authorize]
public class AuthenticatedTool(IHttpContextAccessor httpContextAccessor)
{
    [McpServerTool, Description("Simple check to see what user the MCP is operating from.")]
    public string AnalyzeMCPServerUser()
    {
        try
        {
            DateTime now = DateTime.Now;
            string customFormat = now.ToString("yyyy-MM-dd HH:mm:ss");

            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                // Try multiple claim types for username (OBO tokens may use different claims)
                string? username =
                    user.Identity.Name
                    ?? user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst("preferred_username")?.Value
                    ?? user.FindFirst("upn")?.Value
                    ?? user.FindFirst("email")?.Value
                    ?? user.FindFirst("name")?.Value;

                string authenticationType = user.Identity.AuthenticationType ?? "Unknown";

                // Debug: List all claims for troubleshooting
                var allClaims = string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"));

                var userInfo = $"User: {username ?? "Unknown"}";
                string roles = string.Join(
                    ", ",
                    user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
                );
                if (!string.IsNullOrEmpty(roles))
                {
                    userInfo += $", Roles: {roles}";
                }
                userInfo += $", AuthType: {authenticationType}";
                userInfo += $", Claims: [{allClaims}]";

                return $"MCP server is operational. Authenticated as: {userInfo}. Checked: {customFormat}";
            }
            return $"MCP server is operational but no authenticated user found. Checked: {customFormat}";
        }
        catch (Exception ex)
        {
            throw new McpException($"Error analyzing MCP server user. {ex.Message}", ex);
        }
    }
}
