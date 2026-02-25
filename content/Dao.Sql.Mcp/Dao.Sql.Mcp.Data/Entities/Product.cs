namespace Dao.Sql.Mcp.Data.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsAvailable { get; set; } = true;

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
