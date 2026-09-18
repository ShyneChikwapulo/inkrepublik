using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

public interface IAdminContactService
{
    Task<List<ContactMessage>> GetAllAsync(CancellationToken ct = default);
    Task<ContactMessage?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> SetHandledAsync(int id, bool handled, CancellationToken ct = default);
    Task<bool> UpdateOwnerNotesAsync(int id, string? notes, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public class AdminContactService : IAdminContactService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly ILogger<AdminContactService> _logger;

    public AdminContactService(
        IDbContextFactory<InkrepublikDbContext> factory,
        ILogger<AdminContactService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<ContactMessage>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.ContactMessages
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ContactMessage?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.ContactMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<bool> SetHandledAsync(int id, bool handled, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var msg = await db.ContactMessages.FindAsync([id], ct);
        if (msg is null) return false;

        msg.IsHandled = handled;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateOwnerNotesAsync(int id, string? notes, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var msg = await db.ContactMessages.FindAsync([id], ct);
        if (msg is null) return false;

        msg.OwnerNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var msg = await db.ContactMessages.FindAsync([id], ct);
        if (msg is null) return false;

        db.ContactMessages.Remove(msg);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Contact message {Id} deleted", id);
        return true;
    }
}