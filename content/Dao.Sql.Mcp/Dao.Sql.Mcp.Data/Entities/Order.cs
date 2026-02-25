namespace Dao.Sql.Mcp.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public required string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
