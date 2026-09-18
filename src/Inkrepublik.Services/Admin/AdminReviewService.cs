using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Write-side operations for client reviews.
/// </summary>
public interface IAdminReviewService
{
    Task<List<Review>> GetAllAsync(CancellationToken ct = default);
    Task<Review?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Review> CreateAsync(Review review, CancellationToken ct = default);
    Task<bool> UpdateAsync(Review review, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Quick toggle actions used directly from the list.</summary>
    Task<bool> SetApprovedAsync(int id, bool approved, CancellationToken ct = default);
    Task<bool> SetFeaturedAsync(int id, bool featured, CancellationToken ct = default);
}

public class AdminReviewService : IAdminReviewService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly ILogger<AdminReviewService> _logger;

    public AdminReviewService(
        IDbContextFactory<InkrepublikDbContext> factory,
        ILogger<AdminReviewService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Review>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Reviews
            .AsNoTracking()
            .OrderBy(r => r.DisplayOrder)
            .ThenByDescending(r => r.ReviewedOn ?? r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Review?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Reviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Review> CreateAsync(Review review, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Review by '{Client}' created (id {Id})", review.ClientName, review.Id);
        return review;
    }

    public async Task<bool> UpdateAsync(Review review, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.Reviews.FindAsync([review.Id], ct);
        if (existing is null) return false;

        existing.ClientName = review.ClientName;
        existing.Rating = review.Rating;
        existing.Text = review.Text;
        existing.Source = review.Source;
        existing.ReviewedOn = review.ReviewedOn;
        existing.IsApproved = review.IsApproved;
        existing.IsFeatured = review.IsFeatured;
        existing.DisplayOrder = review.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Review {Id} updated", review.Id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var review = await db.Reviews.FindAsync([id], ct);
        if (review is null) return false;

        db.Reviews.Remove(review);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Review {Id} deleted", id);
        return true;
    }

    public async Task<bool> SetApprovedAsync(int id, bool approved, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var review = await db.Reviews.FindAsync([id], ct);
        if (review is null) return false;

        review.IsApproved = approved;

        // Un-approving automatically un-features — can't feature a rejected review.
        if (!approved) review.IsFeatured = false;

        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetFeaturedAsync(int id, bool featured, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var review = await db.Reviews.FindAsync([id], ct);
        if (review is null) return false;

        // Can only feature approved reviews.
        if (featured && !review.IsApproved) return false;

        review.IsFeatured = featured;
        review.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}