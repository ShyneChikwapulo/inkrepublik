using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Inkrepublik.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Read-only queries for the calendar view. Returns all bookings in a
/// date range, filtered by status and (optionally) artist.
/// </summary>
public interface IAdminCalendarService
{
    Task<List<BookingRequest>> GetBookingsInRangeAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        IReadOnlyCollection<BookingStatus>? statuses = null,
        int? artistId = null,
        CancellationToken ct = default);
}

public class AdminCalendarService : IAdminCalendarService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public AdminCalendarService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<List<BookingRequest>> GetBookingsInRangeAsync(
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        IReadOnlyCollection<BookingStatus>? statuses = null,
        int? artistId = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.BookingRequests
            .AsNoTracking()
            .Include(b => b.Artist)
            .Include(b => b.Service)
            .Where(b => b.PreferredDateTime >= rangeStartUtc
                     && b.PreferredDateTime < rangeEndUtc);

        if (statuses is { Count: > 0 })
        {
            query = query.Where(b => statuses.Contains(b.Status));
        }

        if (artistId.HasValue)
        {
            query = query.Where(b => b.ArtistId == artistId);
        }

        return await query
            .OrderBy(b => b.PreferredDateTime)
            .ToListAsync(ct);
    }
}