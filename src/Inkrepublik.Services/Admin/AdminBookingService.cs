using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Inkrepublik.Domain.Enums;
using Inkrepublik.Services.Bookings;
using Inkrepublik.Services.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Admin-side booking queries and actions. Read queries are AsNoTracking;
/// write actions modify the entity and (for some) send emails.
/// </summary>
public interface IAdminBookingService
{
    // --- Read ---
    Task<List<BookingRequest>> GetAllAsync(
        BookingStatus? status = null,
        string? search = null,
        CancellationToken ct = default);

    Task<BookingRequest?> GetByIdAsync(int id, CancellationToken ct = default);

    // --- Actions ---
    Task<bool> ConfirmAsync(int id, DateTime confirmedDateTime, string? ownerNotes, string publicBaseUrl, CancellationToken ct = default);
    Task<bool> DeclineAsync(int id, string? reason, CancellationToken ct = default);
    Task<bool> MarkCompleteAsync(int id, string publicBaseUrl, CancellationToken ct = default);
    Task<bool> MarkNoShowAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateOwnerNotesAsync(int id, string? notes, CancellationToken ct = default);
}

public class AdminBookingService : IAdminBookingService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly IEmailSender _email;
    private readonly ILogger<AdminBookingService> _logger;

    public AdminBookingService(
        IDbContextFactory<InkrepublikDbContext> factory,
        IEmailSender email,
        ILogger<AdminBookingService> logger)
    {
        _factory = factory;
        _email = email;
        _logger = logger;
    }

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
            query = query.Where(b => b.Status == status.Value);

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

    public async Task<bool> ConfirmAsync(
        int id,
        DateTime confirmedDateTime,
        string? ownerNotes,
        string publicBaseUrl,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null) return false;

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedDateTime = confirmedDateTime;
        booking.OwnerNotes = string.IsNullOrWhiteSpace(ownerNotes) ? null : ownerNotes.Trim();
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {Id} confirmed for {DateTime:u}", id, confirmedDateTime);

        await TrySendAsync(booking,
            b => BookingEmailTemplates.BookingConfirmed(b, confirmedDateTime, BuildStatusUrl(b, publicBaseUrl)),
            ct);

        return true;
    }

    public async Task<bool> DeclineAsync(
        int id,
        string? reason,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null) return false;

        booking.Status = BookingStatus.Declined;
        booking.OwnerNotes = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {Id} declined", id);

        // For decline, we don't need a valid status URL for the client —
        // the booking is dead. Send them a link to /book instead. Pass "" so
        // the template doesn't try to include one.
        await TrySendAsync(booking,
            b => BookingEmailTemplates.BookingDeclined(b, reason, ""),
            ct);

        return true;
    }

    public async Task<bool> MarkCompleteAsync(
        int id,
        string publicBaseUrl,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null) return false;

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {Id} marked complete", id);

        await TrySendAsync(booking, b => BookingEmailTemplates.BookingCompleted(b), ct);

        return true;
    }

    public async Task<bool> MarkNoShowAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null) return false;

        booking.Status = BookingStatus.NoShow;
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {Id} marked no-show", id);
        return true;
    }

    public async Task<bool> UpdateOwnerNotesAsync(int id, string? notes, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null) return false;

        booking.OwnerNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------
    private static string BuildStatusUrl(BookingRequest booking, string publicBaseUrl)
        => $"{publicBaseUrl.TrimEnd('/')}/booking/status/{booking.MagicToken}";

    /// <summary>
    /// Attempts to send an email, logging but not throwing on failure.
    /// The DB write already succeeded — we don't want a failed email to
    /// roll back a state change.
    /// </summary>
    private async Task TrySendAsync(
        BookingRequest booking,
        Func<BookingRequest, EmailMessage> builder,
        CancellationToken ct)
    {
        try
        {
            var message = builder(booking);
            await _email.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email for booking {Id}", booking.Id);
        }
    }
}