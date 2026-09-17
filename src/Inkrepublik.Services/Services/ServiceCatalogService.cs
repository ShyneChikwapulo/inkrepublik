using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Services;

/// <summary>
/// Read-side queries for the studio's service catalog.
/// Named "ServiceCatalogService" to avoid the "ServiceService" naming curse.
/// </summary>
public interface IServiceCatalogService
{
    Task<List<Service>> GetActiveServicesAsync(CancellationToken ct = default);

    Task<List<Service>> GetFeaturedServicesAsync(int count, CancellationToken ct = default);
}

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly InkrepublikDbContext _db;

    public ServiceCatalogService(InkrepublikDbContext db) => _db = db;

    public Task<List<Service>> GetActiveServicesAsync(CancellationToken ct = default)
        => _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

    public Task<List<Service>> GetFeaturedServicesAsync(int count, CancellationToken ct = default)
        => _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Take(count)
            .ToListAsync(ct);
}