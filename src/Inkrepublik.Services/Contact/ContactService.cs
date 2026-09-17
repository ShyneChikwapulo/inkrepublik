using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.Contact;

/// <summary>
/// Handles contact form submissions (general enquiries, not bookings).
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Saves a new contact message. Returns the created entity so the
    /// caller can use its Id for logging or follow-up.
    /// </summary>
    Task<ContactMessage> SubmitAsync(ContactMessage message, CancellationToken ct = default);

    /// <summary>
    /// Messages the studio hasn't dealt with yet. Used by the admin inbox (Phase 5).
    /// </summary>
    Task<List<ContactMessage>> GetUnhandledAsync(CancellationToken ct = default);
}

public class ContactService : IContactService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public ContactService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<ContactMessage> SubmitAsync(ContactMessage message, CancellationToken ct = default)
    {
        // Always set these server-side — never trust the client.
        message.CreatedAt = DateTime.UtcNow;
        message.IsHandled = false;
        message.OwnerNotes = null;

        await using var db = await _factory.CreateDbContextAsync(ct);
        db.ContactMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<List<ContactMessage>> GetUnhandledAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.ContactMessages
            .AsNoTracking()
            .Where(m => !m.IsHandled)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }
}