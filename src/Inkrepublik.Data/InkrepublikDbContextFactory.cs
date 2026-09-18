using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inkrepublik.Data;

/// <summary>
/// Allows the EF Core CLI tools (`dotnet ef migrations ...`) to construct
/// the DbContext at design time without booting the full Blazor app.
///
/// PROVIDER SELECTION:
///   - Set EF_PROVIDER=Sqlite to generate migrations for SQLite.
///   - Otherwise, defaults to SqlServer (for local development).
///
/// The connection string here is ONLY used when running EF CLI commands.
/// At runtime, Program.cs provides the real connection string via DI.
/// </summary>
public class InkrepublikDbContextFactory : IDesignTimeDbContextFactory<InkrepublikDbContext>
{
    public InkrepublikDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("EF_PROVIDER") ?? "SqlServer";

        var optionsBuilder = new DbContextOptionsBuilder<InkrepublikDbContext>();

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            // Design-time connection string. For migrations, EF only reads
            // the schema — it doesn't connect to the DB.
            optionsBuilder.UseSqlite("Data Source=design-time.db");
        }
        else
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=Inkrepublik;User Id=sa;Password=Inkrepublik!Dev2026;TrustServerCertificate=True;MultipleActiveResultSets=True");
        }

        return new InkrepublikDbContext(optionsBuilder.Options);
    }
}