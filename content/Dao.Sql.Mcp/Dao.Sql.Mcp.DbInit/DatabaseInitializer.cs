using Dao.Sql.Mcp.Data;
using Dao.Sql.Mcp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dao.Sql.Mcp.DbInit;

/// <summary>
/// Background service that initializes and seeds the database on startup
/// </summary>
public class DatabaseInitializer(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializer> logger,
    IHostApplicationLifetime lifetime
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            logger.LogInformation("Ensuring database is created...");
            await context.Database.EnsureCreatedAsync(cancellationToken);

            logger.LogInformation("Seeding database...");
            await SeedDataAsync(context, cancellationToken);

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
        finally
        {
            // Stop the application after initialization
            lifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedDataAsync(
        AppDbContext context,
        CancellationToken cancellationToken
    )
    {
        // Seed Users
        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var users = new[]
            {
                new User
                {
                    Name = "Alice Johnson",
                    Email = "alice.johnson@example.com",
                    Role = "admin",
                    Active = true,
                },
                new User
                {
                    Name = "Bob Smith",
                    Email = "bob.smith@example.com",
                    Role = "user",
                    Active = true,
                },
                new User
                {
                    Name = "Carol Williams",
                    Email = "carol.williams@example.com",
                    Role = "manager",
                    Active = true,
                },
                new User
                {
                    Name = "David Brown",
                    Email = "david.brown@example.com",
                    Role = "user",
                    Active = true,
                },
                new User
                {
                    Name = "Eve Davis",
                    Email = "eve.davis@example.com",
                    Role = "user",
                    Active = false,
                },
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Seed Products
        if (!await context.Products.AnyAsync(cancellationToken))
        {
            var products = new[]
            {
                new Product
                {
                    Name = "Laptop Pro 15",
                    Description =
                        "High-performance laptop with 15-inch display, 16GB RAM, 512GB SSD",
                    Category = "Electronics",
                    Price = 1299.99m,
                    Stock = 25,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse with 6 programmable buttons",
                    Category = "Accessories",
                    Price = 29.99m,
                    Stock = 150,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "USB-C Hub",
                    Description = "7-in-1 USB-C hub with HDMI, USB 3.0, SD card reader",
                    Category = "Accessories",
                    Price = 49.99m,
                    Stock = 80,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Mechanical Keyboard",
                    Description = "RGB backlit mechanical keyboard with Cherry MX switches",
                    Category = "Accessories",
                    Price = 129.99m,
                    Stock = 45,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "27\" Monitor",
                    Description = "27-inch 4K UHD monitor with HDR support",
                    Category = "Electronics",
                    Price = 449.99m,
                    Stock = 15,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Laptop Stand",
                    Description = "Adjustable aluminum laptop stand with cooling",
                    Category = "Accessories",
                    Price = 39.99m,
                    Stock = 60,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Webcam HD",
                    Description = "1080p HD webcam with noise-canceling microphone",
                    Category = "Electronics",
                    Price = 79.99m,
                    Stock = 35,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "External SSD 1TB",
                    Description = "Portable 1TB SSD with USB 3.2 Gen 2",
                    Category = "Storage",
                    Price = 119.99m,
                    Stock = 50,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Desk Lamp LED",
                    Description = "Adjustable LED desk lamp with USB charging port",
                    Category = "Office",
                    Price = 34.99m,
                    Stock = 75,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Wireless Headphones",
                    Description = "Noise-canceling over-ear wireless headphones",
                    Category = "Audio",
                    Price = 199.99m,
                    Stock = 40,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Phone Charger",
                    Description = "Fast wireless charging pad for smartphones",
                    Category = "Accessories",
                    Price = 24.99m,
                    Stock = 100,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "HDMI Cable 6ft",
                    Description = "Premium HDMI 2.1 cable supporting 4K@120Hz",
                    Category = "Cables",
                    Price = 19.99m,
                    Stock = 200,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Laptop Bag",
                    Description = "Padded laptop bag with multiple compartments",
                    Category = "Accessories",
                    Price = 44.99m,
                    Stock = 55,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "USB Flash Drive 64GB",
                    Description = "High-speed USB 3.0 flash drive",
                    Category = "Storage",
                    Price = 14.99m,
                    Stock = 250,
                    IsAvailable = true,
                },
                new Product
                {
                    Name = "Bluetooth Speaker",
                    Description = "Portable Bluetooth speaker with 12-hour battery",
                    Category = "Audio",
                    Price = 59.99m,
                    Stock = 65,
                    IsAvailable = true,
                },
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Seed Orders and OrderItems
        if (!await context.Orders.AnyAsync(cancellationToken))
        {
            var users = await context.Users.ToListAsync(cancellationToken);
            var products = await context.Products.ToListAsync(cancellationToken);

            var orders = new List<Order>
            {
                new Order
                {
                    UserId = users[1].Id, // Bob
                    OrderDate = DateTime.UtcNow.AddDays(-10),
                    Status = "delivered",
                    TotalAmount = 1359.97m,
                    Notes = "Customer requested gift wrapping",
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[0].Id,
                            Quantity = 1,
                            UnitPrice = 1299.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[1].Id,
                            Quantity = 2,
                            UnitPrice = 29.99m,
                        },
                    ],
                },
                new Order
                {
                    UserId = users[1].Id, // Bob
                    OrderDate = DateTime.UtcNow.AddDays(-5),
                    Status = "shipped",
                    TotalAmount = 84.98m,
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[2].Id,
                            Quantity = 1,
                            UnitPrice = 49.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[8].Id,
                            Quantity = 1,
                            UnitPrice = 34.99m,
                        },
                    ],
                },
                new Order
                {
                    UserId = users[2].Id, // Carol
                    OrderDate = DateTime.UtcNow.AddDays(-8),
                    Status = "delivered",
                    TotalAmount = 619.96m,
                    Notes = "Express shipping",
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[4].Id,
                            Quantity = 1,
                            UnitPrice = 449.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[3].Id,
                            Quantity = 1,
                            UnitPrice = 129.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[11].Id,
                            Quantity = 2,
                            UnitPrice = 19.99m,
                        },
                    ],
                },
                new Order
                {
                    UserId = users[3].Id, // David
                    OrderDate = DateTime.UtcNow.AddDays(-3),
                    Status = "processing",
                    TotalAmount = 319.98m,
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[9].Id,
                            Quantity = 1,
                            UnitPrice = 199.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[7].Id,
                            Quantity = 1,
                            UnitPrice = 119.99m,
                        },
                    ],
                },
                new Order
                {
                    UserId = users[3].Id, // David
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    Status = "pending",
                    TotalAmount = 79.99m,
                    Notes = "Customer will provide shipping address later",
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[6].Id,
                            Quantity = 1,
                            UnitPrice = 79.99m,
                        },
                    ],
                },
                new Order
                {
                    UserId = users[1].Id, // Bob
                    OrderDate = DateTime.UtcNow.AddDays(-15),
                    Status = "delivered",
                    TotalAmount = 199.99m,
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[9].Id,
                            Quantity = 1,
                            UnitPrice = 199.99m,
                        },
                    ],
                },
                new Order
                {
                    UserId = users[2].Id, // Carol
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    Status = "shipped",
                    TotalAmount = 269.94m,
                    Notes = "Office supplies order",
                    OrderItems =
                    [
                        new OrderItem
                        {
                            ProductId = products[12].Id,
                            Quantity = 1,
                            UnitPrice = 44.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[13].Id,
                            Quantity = 5,
                            UnitPrice = 14.99m,
                        },
                        new OrderItem
                        {
                            ProductId = products[8].Id,
                            Quantity = 4,
                            UnitPrice = 34.99m,
                        },
                    ],
                },
            };

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
