using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dao.Aspire.Mcp.Data;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool IsAvailable { get; set; } = true;

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
