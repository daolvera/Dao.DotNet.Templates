using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dao.Aspire.Mcp.Server.Prompts;

[McpServerPromptType]
public class BasicPrompt
{
    [McpServerPrompt(Name = "BasicPromp"), Description("A prompt to interact with the test server")]
    public static IEnumerable<ChatMessage> BasicPromptExample(
        [Description("A random int")] int number
        )
    {
        return [
            new (ChatRole.User, "This is a prompt"),
            new (ChatRole.Assistant, $"I understand, here is your number: {number}")
            ];
    }
}
