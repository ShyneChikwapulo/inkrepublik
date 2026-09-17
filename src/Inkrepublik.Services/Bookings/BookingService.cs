using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Inkrepublik.Domain.Enums;
using Inkrepublik.Services.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Bookings;

public interface IBookingService
{

    /// <summary>
    /// Client-initiated reschedule via magic link. Only Pending or Confirmed
    /// bookings can be rescheduled by the client.
    /// </summary>
    Task<bool> RescheduleByTokenAsync(
        string token,
        DateTime newPreferredDateTime,
        CancellationToken ct = default);

    /// <summary>
    /// Submits a new booking request. Saves it as Pending with a fresh magic
    /// token, then sends two emails: owner notification + client confirmation
    /// (with the magic link).
    /// </summary>
    Task<BookingRequest> SubmitAsync(
        BookingRequestInput input,
        string ownerEmail,
        string publicBaseUrl,
        CancellationToken ct = default);

    /// <summary>Fetch a booking by its magic token, if the token is still valid.</summary>
    Task<BookingRequest?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Client-initiated cancellation via magic link. Only Pending or Confirmed bookings can be cancelled.</summary>
    Task<bool> CancelByTokenAsync(string token, CancellationToken ct = default);

    // --- Used by the admin panel (Phase 5) ---
    Task<List<BookingRequest>> GetAllAsync(CancellationToken ct = default);
}

public class BookingService : IBookingService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly IEmailSender _email;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IDbContextFactory<InkrepublikDbContext> factory,
        IEmailSender email,
        ILogger<BookingService> logger)
    {
        _factory = factory;
        _email = email;
        _logger = logger;
    }

    public async Task<BookingRequest> SubmitAsync(
        BookingRequestInput input,
        string ownerEmail,
        string publicBaseUrl,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var booking = new BookingRequest
        {
            ClientName = input.ClientName.Trim(),
            ClientEmail = input.ClientEmail.Trim(),
            ClientPhone = string.IsNullOrWhiteSpace(input.ClientPhone) ? null : input.ClientPhone.Trim(),
            ArtistId = input.ArtistId,
            ServiceId = input.ServiceId,
            PreferredDateTime = input.PreferredDateTime,
            Description = input.Description.Trim(),

            Status = BookingStatus.Pending,
            MagicToken = MagicTokenGenerator.NewToken(),
            MagicTokenExpiresAt = now.AddDays(30),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using var db = await _factory.CreateDbContextAsync(ct);
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Booking request {Id} created for {Client} ({Email}), preferred {DateTime:u}",
            booking.Id, booking.ClientName, booking.ClientEmail, booking.PreferredDateTime);

        // Send emails — but don't fail the whole booking if they fail.
        // The record is saved; the studio can see it in admin even if email is down.
        await TrySendEmailsAsync(booking, ownerEmail, publicBaseUrl, ct);

        return booking;
    }

    public async Task<BookingRequest?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests
            .AsNoTracking()
            .Include(b => b.Artist)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.MagicToken == token, ct);

        if (booking is null) return null;
        if (booking.MagicTokenExpiresAt < DateTime.UtcNow) return null;

        return booking;
    }

    public async Task<bool> CancelByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests
            .FirstOrDefaultAsync(b => b.MagicToken == token, ct);

        if (booking is null) return false;
        if (booking.MagicTokenExpiresAt < DateTime.UtcNow) return false;

        // Only Pending or Confirmed bookings can be cancelled by the client.
        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            return false;

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {Id} cancelled via magic link.", booking.Id);
        return true;
    }

    public async Task<bool> RescheduleByTokenAsync(
        string token,
        DateTime newPreferredDateTime,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        await using var db = await _factory.CreateDbContextAsync(ct);
        var booking = await db.BookingRequests
            .FirstOrDefaultAsync(b => b.MagicToken == token, ct);

        if (booking is null) return false;
        if (booking.MagicTokenExpiresAt < DateTime.UtcNow) return false;

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            return false;

        // Changing the preferred time returns the booking to Pending,
        // so the owner re-confirms. This is deliberate — a client can't
        // unilaterally move an already-confirmed appointment.
        booking.PreferredDateTime = newPreferredDateTime;
        booking.Status = BookingStatus.Pending;
        booking.ConfirmedDateTime = null;
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Booking {Id} rescheduled to {DateTime:u} by client via magic link.",
            booking.Id, newPreferredDateTime);

        return true;
    }
    public async Task<List<BookingRequest>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.BookingRequests
            .AsNoTracking()
            .Include(b => b.Artist)
            .Include(b => b.Service)
            .OrderByDescending(b => b.PreferredDateTime)
            .ToListAsync(ct);
    }

    // ----------------------------------------------------------------
    // Email helpers
    // ----------------------------------------------------------------
    private async Task TrySendEmailsAsync(
        BookingRequest booking,
        string ownerEmail,
        string publicBaseUrl,
        CancellationToken ct)
    {
        var statusUrl = $"{publicBaseUrl.TrimEnd('/')}/booking/status/{booking.MagicToken}";

        try
        {
            var ownerMessage = BookingEmailTemplates.OwnerNotification(booking, statusUrl);
            await _email.SendAsync(ownerMessage with { To = ownerEmail, ToName = "Inkrepublik Studio" }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send owner notification for booking {Id}", booking.Id);
        }

        try
        {
            var clientMessage = BookingEmailTemplates.ClientConfirmation(booking, statusUrl);
            await _email.SendAsync(clientMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send client confirmation for booking {Id}", booking.Id);
        }
    }
}