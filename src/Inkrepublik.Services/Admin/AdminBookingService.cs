using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Inkrepublik.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Admin-side booking queries. Write operations (confirm/decline/etc.) land
/// in Phase 5C.
/// </summary>
public interface IAdminBookingService
{
    Task<List<BookingRequest>> GetAllAsync(
        BookingStatus? status = null,
        string? search = null,
        CancellationToken ct = default);

    Task<BookingRequest?> GetByIdAsync(int id, CancellationToken ct = default);
}

public class AdminBookingService : IAdminBookingService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public AdminBookingService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<List<BookingRequest>> GetAllAsync(
        BookingStatus? status = null,
        string? search = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.BookingRequests
            .AsNoTracking()
            .Include(b => b.Artist)
            .Include(b => b.Service)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(b =>
                b.ClientName.ToLower().Contains(term) ||
                b.ClientEmail.ToLower().Contains(term) ||
                (b.ClientPhone != null && b.ClientPhone.Contains(term)));
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<BookingRequest?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.BookingRequests
            .AsNoTracking()
            .Include(b => b.Artist)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }
}