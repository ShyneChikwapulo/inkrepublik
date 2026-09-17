using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Artists;

/// <summary>
/// Read-side queries for artists. Write operations live in the admin
/// service (Phase 5).
/// </summary>
public interface IArtistService
{
    /// <summary>Active artists, ordered by DisplayOrder.</summary>
    Task<List<Artist>> GetActiveArtistsAsync(CancellationToken ct = default);

    /// <summary>Top N active artists — for home page preview.</summary>
    Task<List<Artist>> GetFeaturedArtistsAsync(int count, CancellationToken ct = default);

    /// <summary>Single artist by id, including portfolio images.</summary>
    Task<Artist?> GetArtistWithPortfolioAsync(int id, CancellationToken ct = default);
}

public class ArtistService : IArtistService
{
    private readonly InkrepublikDbContext _db;

    public ArtistService(InkrepublikDbContext db) => _db = db;

    public Task<List<Artist>> GetActiveArtistsAsync(CancellationToken ct = default)
        => _db.Artists
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);

    public Task<List<Artist>> GetFeaturedArtistsAsync(int count, CancellationToken ct = default)
        => _db.Artists
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .Take(count)
            .ToListAsync(ct);

    public Task<Artist?> GetArtistWithPortfolioAsync(int id, CancellationToken ct = default)
        => _db.Artists
            .AsNoTracking()
            .Include(a => a.PortfolioImages.OrderBy(p => p.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}