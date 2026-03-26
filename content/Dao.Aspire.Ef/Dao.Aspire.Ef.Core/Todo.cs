using System.ComponentModel.DataAnnotations;

namespace Dao.Aspire.Ef.Core;

/// <summary>
/// Simple Entity to demonstrate a migration
/// </summary>
public class Todo
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required DateTime DueDate { get; set; }
    public string? Description { get; set; }
}
