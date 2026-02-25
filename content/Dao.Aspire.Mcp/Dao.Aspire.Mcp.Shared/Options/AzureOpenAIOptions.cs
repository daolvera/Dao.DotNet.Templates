using System.ComponentModel.DataAnnotations;

namespace Dao.Aspire.Mcp.Shared.Options;

public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";
    [Required]
    public required string Endpoint { get; set; }
    public string? ApiKey { get; set; }
    [Required]
    public required string DeploymentName { get; set; }
}
