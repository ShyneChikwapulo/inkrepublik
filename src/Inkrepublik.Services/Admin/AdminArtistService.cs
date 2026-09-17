using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Write-side operations for artists and their portfolio images.
/// </summary>
public interface IAdminArtistService
{
    Task<List<Artist>> GetAllAsync(CancellationToken ct = default);
    Task<Artist?> GetByIdWithPortfolioAsync(int id, CancellationToken ct = default);
    Task<Artist> CreateAsync(Artist artist, CancellationToken ct = default);
    Task<bool> UpdateAsync(Artist artist, CancellationToken ct = default);
    Task<(bool Success, string? Reason)> DeleteAsync(int id, CancellationToken ct = default);

    // Portfolio images
    Task<ArtistImage> AddPortfolioImageAsync(int artistId, string imagePath, string? caption, CancellationToken ct = default);
    Task<bool> DeletePortfolioImageAsync(int imageId, CancellationToken ct = default);
    Task<bool> UpdatePortfolioCaptionAsync(int imageId, string? caption, CancellationToken ct = default);
    Task<bool> ReorderPortfolioImagesAsync(int artistId, IReadOnlyList<int> orderedImageIds, CancellationToken ct = default);
}

public class AdminArtistService : IAdminArtistService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly ILogger<AdminArtistService> _logger;

    public AdminArtistService(
        IDbContextFactory<InkrepublikDbContext> factory,
        ILogger<AdminArtistService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Artist>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Artists
            .AsNoTracking()
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<Artist?> GetByIdWithPortfolioAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Artists
            .AsNoTracking()
            .Include(a => a.PortfolioImages.OrderBy(p => p.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Artist> CreateAsync(Artist artist, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        artist.CreatedAt = DateTime.UtcNow;
        artist.UpdatedAt = DateTime.UtcNow;

        db.Artists.Add(artist);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Artist '{Name}' created (id {Id})", artist.Name, artist.Id);
        return artist;
    }

    public async Task<bool> UpdateAsync(Artist artist, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.Artists.FindAsync([artist.Id], ct);
        if (existing is null) return false;

        existing.Name = artist.Name;
        existing.Title = artist.Title;
        existing.Bio = artist.Bio;
        existing.Specialties = artist.Specialties;
        existing.InstagramHandle = artist.InstagramHandle;
        existing.ProfileImagePath = artist.ProfileImagePath;
        existing.IsActive = artist.IsActive;
        existing.DisplayOrder = artist.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Artist {Id} updated", artist.Id);
        return true;
    }

    public async Task<(bool Success, string? Reason)> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var artist = await db.Artists.FindAsync([id], ct);
        if (artist is null) return (false, "Artist not found.");

        var bookingCount = await db.BookingRequests
            .CountAsync(b => b.ArtistId == id, ct);

        if (bookingCount > 0)
        {
            return (false,
                $"This artist has {bookingCount} booking(s). Deactivate them instead of deleting.");
        }

        db.Artists.Remove(artist);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Artist {Id} deleted", id);
        return (true, null);
    }

    public async Task<ArtistImage> AddPortfolioImageAsync(
        int artistId,
        string imagePath,
        string? caption,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var maxOrder = await db.ArtistImages
            .Where(ai => ai.ArtistId == artistId)
            .Select(ai => (int?)ai.DisplayOrder)
            .MaxAsync(ct) ?? 0;

        var image = new ArtistImage
        {
            ArtistId = artistId,
            ImagePath = imagePath,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            DisplayOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
        };

        db.ArtistImages.Add(image);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Portfolio image added to artist {ArtistId} (image {ImageId})", artistId, image.Id);
        return image;
    }

    public async Task<bool> DeletePortfolioImageAsync(int imageId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var image = await db.ArtistImages.FindAsync([imageId], ct);
        if (image is null) return false;

        db.ArtistImages.Remove(image);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Portfolio image {Id} deleted", imageId);
        return true;
    }

    public async Task<bool> UpdatePortfolioCaptionAsync(int imageId, string? caption, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var image = await db.ArtistImages.FindAsync([imageId], ct);
        if (image is null) return false;

        image.Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReorderPortfolioImagesAsync(
        int artistId,
        IReadOnlyList<int> orderedImageIds,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var images = await db.ArtistImages
            .Where(ai => ai.ArtistId == artistId && orderedImageIds.Contains(ai.Id))
            .ToListAsync(ct);

        for (var i = 0; i < orderedImageIds.Count; i++)
        {
            var img = images.FirstOrDefault(x => x.Id == orderedImageIds[i]);
            if (img is not null) img.DisplayOrder = i + 1;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}