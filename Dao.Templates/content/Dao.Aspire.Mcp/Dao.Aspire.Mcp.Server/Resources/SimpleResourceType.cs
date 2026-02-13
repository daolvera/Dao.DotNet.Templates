using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dao.Aspire.Mcp.Server.Resources;

[McpServerResourceType]
public class SimpleResourceType
{
    private const int MagicNumber = 42;

    [McpServerResource(Name = "SimpleResource", MimeType = "text/plain")]
    [Description("A simple resource to test resource functionality")]
    public static string DirectTextResource()
    {
        return $"Here is your answer: {MagicNumber}";
    }
}
