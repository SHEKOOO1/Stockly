using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stockly.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model for migrations without
/// booting the API.
/// <para>
/// The connection string here is a <strong>placeholder that is never opened</strong>:
/// EF Core only needs a provider to compile the model, so migration scaffolding works
/// on a machine with no SQL Server. It must not be a real credential, and
/// <c>migrations add</c> must never be run against it (AGENTS.md §4).
/// </para>
/// </summary>
public sealed class DesignTimeStocklyDbContextFactory : IDesignTimeDbContextFactory<StocklyDbContext>
{
    private const string Placeholder = "Server=(localdb)\\StocklyDesignTime;Database=Stockly;Trusted_Connection=True;TrustServerCertificate=True";

    public StocklyDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<StocklyDbContext>()
            .UseSqlServer(args.Length > 0 ? args[0] : Placeholder);

        // Platform scope: no tenant query filter, so migrations see the full model.
        return new StocklyDbContext(builder.Options, new FixedTenantContext(null, null, isPlatformScope: true));
    }
}