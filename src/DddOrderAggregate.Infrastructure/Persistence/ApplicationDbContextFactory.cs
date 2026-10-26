using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DddOrderAggregate.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext to enable EF Core migration generation via dotnet-ef tooling.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite("Data Source=orders.db");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
