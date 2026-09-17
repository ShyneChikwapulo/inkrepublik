using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Write-side operations for the service catalog (admin only).
/// </summary>
public interface IAdminServiceCatalogService
{
    Task<List<Service>> GetAllAsync(CancellationToken ct = default);

    Task<Service?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Service> CreateAsync(Service service, CancellationToken ct = default);

    Task<bool> UpdateAsync(Service service, CancellationToken ct = default);

    /// <summary>
    /// Deletes the service. Returns (success, reason).
    /// Fails if any booking references the service.
    /// </summary>
    Task<(bool Success, string? Reason)> DeleteAsync(int id, CancellationToken ct = default);
}

public class AdminServiceCatalogService : IAdminServiceCatalogService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly ILogger<AdminServiceCatalogService> _logger;

    public AdminServiceCatalogService(
        IDbContextFactory<InkrepublikDbContext> factory,
        ILogger<AdminServiceCatalogService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Service>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Services
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<Service?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Service> CreateAsync(Service service, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        service.CreatedAt = DateTime.UtcNow;
        service.UpdatedAt = DateTime.UtcNow;

        db.Services.Add(service);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Service '{Name}' created (id {Id})", service.Name, service.Id);
        return service;
    }

    public async Task<bool> UpdateAsync(Service service, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.Services.FindAsync([service.Id], ct);
        if (existing is null) return false;

        existing.Name = service.Name;
        existing.Description = service.Description;
        existing.Category = service.Category;
        existing.PriceFrom = service.PriceFrom;
        existing.PriceTo = service.PriceTo;
        existing.TypicalDurationMinutes = service.TypicalDurationMinutes;
        existing.IsActive = service.IsActive;
        existing.DisplayOrder = service.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Service {Id} updated", service.Id);
        return true;
    }

    public async Task<(bool Success, string? Reason)> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var service = await db.Services.FindAsync([id], ct);
        if (service is null) return (false, "Service not found.");

        var referenceCount = await db.BookingRequests
            .CountAsync(b => b.ServiceId == id, ct);

        if (referenceCount > 0)
        {
            return (false,
                $"This service is referenced by {referenceCount} booking(s). " +
                "Deactivate it instead of deleting.");
        }

        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Service {Id} deleted", id);
        return (true, null);
    }
}