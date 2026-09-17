using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Reviews;

/// <summary>
/// Read-side queries for reviews. Only approved reviews are returned
/// for public display.
/// </summary>
public interface IReviewService
{
    Task<List<Review>> GetApprovedReviewsAsync(CancellationToken ct = default);

    Task<List<Review>> GetFeaturedReviewsAsync(int count, CancellationToken ct = default);
}

public class ReviewService : IReviewService
{
    private readonly InkrepublikDbContext _db;

    public ReviewService(InkrepublikDbContext db) => _db = db;

    public Task<List<Review>> GetApprovedReviewsAsync(CancellationToken ct = default)
        => _db.Reviews
            .AsNoTracking()
            .Where(r => r.IsApproved)
            .OrderBy(r => r.DisplayOrder)
            .ThenByDescending(r => r.ReviewedOn ?? r.CreatedAt)
            .ToListAsync(ct);

    public Task<List<Review>> GetFeaturedReviewsAsync(int count, CancellationToken ct = default)
        => _db.Reviews
            .AsNoTracking()
            .Where(r => r.IsApproved && r.IsFeatured)
            .OrderBy(r => r.DisplayOrder)
            .Take(count)
            .ToListAsync(ct);
}