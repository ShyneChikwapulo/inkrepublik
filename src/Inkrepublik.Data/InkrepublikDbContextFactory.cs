using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inkrepublik.Data;

/// <summary>
/// Allows the EF Core CLI tools (`dotnet ef migrations ...`) to construct
/// the DbContext at design time without booting the full Blazor app.
///
/// The connection string here is ONLY used when running EF CLI commands.
/// At runtime, the app's Program.cs provides the real connection string
/// via dependency injection.
/// </summary>
public class InkrepublikDbContextFactory : IDesignTimeDbContextFactory<InkrepublikDbContext>
{
    public InkrepublikDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=localhost,1433;Database=Inkrepublik;User Id=sa;Password=Inkrepublik!Dev2026;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var optionsBuilder = new DbContextOptionsBuilder<InkrepublikDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new InkrepublikDbContext(optionsBuilder.Options);
    }
}