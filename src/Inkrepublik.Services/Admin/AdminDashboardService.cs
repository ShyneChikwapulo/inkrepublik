using Inkrepublik.Data;
using Inkrepublik.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Read-only aggregate metrics for the admin dashboard.
/// All queries are cheap — indexes already exist on Status and CreatedAt.
/// </summary>
public interface IAdminDashboardService
{
    Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);
}

public record DashboardStats(
    int PendingBookings,
    int ConfirmedUpcoming,
    int TodayBookings,
    int UnreadMessages,
    int PendingReviews,
    int TotalArtists,
    int TotalServices);

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public AdminDashboardService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<DashboardStats> GetStatsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        var pending = await db.BookingRequests
            .CountAsync(b => b.Status == BookingStatus.Pending, ct);

        var confirmedUpcoming = await db.BookingRequests
            .CountAsync(b => b.Status == BookingStatus.Confirmed
                          && b.PreferredDateTime >= now, ct);

        var todayBookings = await db.BookingRequests
            .CountAsync(b => (b.Status == BookingStatus.Pending
                           || b.Status == BookingStatus.Confirmed)
                          && b.PreferredDateTime >= todayStart
                          && b.PreferredDateTime < todayEnd, ct);

        var unreadMessages = await db.ContactMessages
            .CountAsync(m => !m.IsHandled, ct);

        var pendingReviews = await db.Reviews
            .CountAsync(r => !r.IsApproved, ct);

        var totalArtists = await db.Artists
            .CountAsync(a => a.IsActive, ct);

        var totalServices = await db.Services
            .CountAsync(s => s.IsActive, ct);

        return new DashboardStats(
            pending,
            confirmedUpcoming,
            todayBookings,
            unreadMessages,
            pendingReviews,
            totalArtists,
            totalServices);
    }
}