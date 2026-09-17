using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Reviews;

public interface IReviewService
{
    Task<List<Review>> GetApprovedReviewsAsync(CancellationToken ct = default);
    Task<List<Review>> GetFeaturedReviewsAsync(int count, CancellationToken ct = default);
}

public class ReviewService : IReviewService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public ReviewService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<List<Review>> GetApprovedReviewsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Reviews
            .AsNoTracking()
            .Where(r => r.IsApproved)
            .OrderBy(r => r.DisplayOrder)
            .ThenByDescending(r => r.ReviewedOn ?? r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Review>> GetFeaturedReviewsAsync(int count, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Reviews
            .AsNoTracking()
            .Where(r => r.IsApproved && r.IsFeatured)
            .OrderBy(r => r.DisplayOrder)
            .Take(count)
            .ToListAsync(ct);
    }
}