using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Services;

public interface IServiceCatalogService
{
    Task<List<Service>> GetActiveServicesAsync(CancellationToken ct = default);
    Task<List<Service>> GetFeaturedServicesAsync(int count, CancellationToken ct = default);
}

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public ServiceCatalogService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<List<Service>> GetActiveServicesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<List<Service>> GetFeaturedServicesAsync(int count, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Take(count)
            .ToListAsync(ct);
    }
}