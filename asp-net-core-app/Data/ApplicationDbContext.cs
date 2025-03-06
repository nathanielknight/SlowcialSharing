using Microsoft.EntityFrameworkCore;

namespace SlowcialSharing.Data;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Site> Sites { get; set; }
    public DbSet<Item> Items { get; set; }
}
