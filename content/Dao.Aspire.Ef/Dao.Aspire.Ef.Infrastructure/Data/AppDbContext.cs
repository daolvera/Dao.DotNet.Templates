using Dao.Aspire.Ef.Core;
using Microsoft.EntityFrameworkCore;

namespace Dao.Aspire.Ef.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Todo> Todos => Set<Todo>();
}
