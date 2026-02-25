using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dao.Aspire.Mcp.Data;

public static class DbInitializer
{
    /// <summary>
    /// Initializes the database by ensuring it's created and optionally seeding data
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool seedData = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            logger.LogInformation("Ensuring database is created...");

            // Ensure database is created (for development scenarios)
            // In production, use migrations instead
            await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Database created successfully");

            if (seedData && !context.Users.Any())
            {
                logger.LogInformation("Seeding initial data...");
                await SeedDataAsync(context);
                logger.LogInformation("Data seeded successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private static async Task SeedDataAsync(AppDbContext context)
    {
        // Seed Users
        var users = new[]
        {
            new User
            {
                Name = "Admin User",
                Email = "admin@example.com",
                Role = "Admin",
                Active = true,
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Role = "User",
                Active = true,
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Name = "Jane Smith",
                Email = "jane.smith@example.com",
                Role = "User",
                Active = true,
                CreatedAt = DateTime.UtcNow,
            },
        };
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Seed Products
        var products = new[]
        {
            new Product
            {
                Name = "Laptop",
                Description = "High-performance laptop for professionals",
                Category = "Electronics",
                Price = 1299.99m,
                Stock = 50,
                IsAvailable = true,
            },
            new Product
            {
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with precision tracking",
                Category = "Electronics",
                Price = 29.99m,
                Stock = 150,
                IsAvailable = true,
            },
            new Product
            {
                Name = "Desk Chair",
                Description = "Comfortable office chair with lumbar support",
                Category = "Furniture",
                Price = 249.99m,
                Stock = 30,
                IsAvailable = true,
            },
            new Product
            {
                Name = "Monitor",
                Description = "27-inch 4K UHD monitor",
                Category = "Electronics",
                Price = 399.99m,
                Stock = 25,
                IsAvailable = true,
            },
            new Product
            {
                Name = "Keyboard",
                Description = "Mechanical keyboard with RGB lighting",
                Category = "Electronics",
                Price = 89.99m,
                Stock = 75,
                IsAvailable = true,
            },
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Seed Orders
        var orders = new[]
        {
            new Order
            {
                UserId = users[1].Id,
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Status = "Completed",
                TotalAmount = 1329.98m,
                Notes = "First order - express delivery",
            },
            new Order
            {
                UserId = users[2].Id,
                OrderDate = DateTime.UtcNow.AddDays(-2),
                Status = "Pending",
                TotalAmount = 649.98m,
                Notes = "Standard delivery",
            },
            new Order
            {
                UserId = users[1].Id,
                OrderDate = DateTime.UtcNow.AddDays(-1),
                Status = "Processing",
                TotalAmount = 89.99m,
                Notes = "",
            },
        };
        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();

        // Seed OrderItems
        var orderItems = new[]
        {
            // First order items
            new OrderItem
            {
                OrderId = orders[0].Id,
                ProductId = products[0].Id,
                Quantity = 1,
                UnitPrice = products[0].Price,
            },
            new OrderItem
            {
                OrderId = orders[0].Id,
                ProductId = products[1].Id,
                Quantity = 1,
                UnitPrice = products[1].Price,
            },
            // Second order items
            new OrderItem
            {
                OrderId = orders[1].Id,
                ProductId = products[2].Id,
                Quantity = 1,
                UnitPrice = products[2].Price,
            },
            new OrderItem
            {
                OrderId = orders[1].Id,
                ProductId = products[3].Id,
                Quantity = 1,
                UnitPrice = products[3].Price,
            },
            // Third order item
            new OrderItem
            {
                OrderId = orders[2].Id,
                ProductId = products[4].Id,
                Quantity = 1,
                UnitPrice = products[4].Price,
            },
        };
        context.OrderItems.AddRange(orderItems);
        await context.SaveChangesAsync();
    }
}
