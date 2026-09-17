using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Artists;

/// <summary>
/// Read-side queries for artists. Write operations live in the admin
/// service (Phase 5).
///
/// Uses IDbContextFactory so each query gets its own short-lived DbContext.
/// This is required in Blazor Server, where scoped services live for the
/// whole circuit and cannot safely share a DbContext across concurrent
/// component initialization.
/// </summary>
/// 
/// <summary>
/// Lightweight projection for gallery tiles — avoids loading full artists
/// and all their collections just to render a thumbnail.
/// </summary>
public record GalleryItem(
    int ArtistId,
    string ArtistName,
    string ImagePath,
    string? Caption,
    int DisplayOrder);
public interface IArtistService
{
    Task<List<Artist>> GetActiveArtistsAsync(CancellationToken ct = default);
    Task<List<Artist>> GetFeaturedArtistsAsync(int count, CancellationToken ct = default);
    Task<Artist?> GetArtistWithPortfolioAsync(int id, CancellationToken ct = default);
    Task<List<GalleryItem>> GetAllPortfolioImagesAsync(CancellationToken ct = default);
}

public class ArtistService : IArtistService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public ArtistService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<List<Artist>> GetActiveArtistsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Artists
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<List<Artist>> GetFeaturedArtistsAsync(int count, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Artists
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<Artist?> GetArtistWithPortfolioAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Artists
            .AsNoTracking()
            .Include(a => a.PortfolioImages.OrderBy(p => p.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<GalleryItem>> GetAllPortfolioImagesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.ArtistImages
            .AsNoTracking()
            .Where(ai => ai.Artist.IsActive)
            .OrderBy(ai => ai.Artist.DisplayOrder)
            .ThenBy(ai => ai.DisplayOrder)
            .Select(ai => new GalleryItem(
                ai.ArtistId,
                ai.Artist.Name,
                ai.ImagePath,
                ai.Caption,
                ai.DisplayOrder))
            .ToListAsync(ct);
    }
}