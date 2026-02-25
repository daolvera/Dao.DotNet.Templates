using System.ComponentModel.DataAnnotations;

namespace Dao.Aspire.Mcp.Shared.Options;

public class AzureAdOptions
{
    public const string SectionName = "AzureAd";
    [Required]
    public required string Instance { get; set; }
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string TenantId { get; set; }
    [Required]
    public required string[] Scopes { get; set; }
    public string? RedirectUri { get; set; }
    public string? Audience { get; set; }
    public string? ClientSecret { get; set; }
    public string? Domain { get; set; }
    public string[]? ClientCertificates { get; set; }
}
