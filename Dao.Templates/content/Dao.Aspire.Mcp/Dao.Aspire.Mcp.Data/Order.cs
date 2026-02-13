using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dao.Aspire.Mcp.Data;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}
