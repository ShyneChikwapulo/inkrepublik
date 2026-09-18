using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

public interface IAdminSiteSettingService
{
    Task<List<SiteSetting>> GetAllAsync(CancellationToken ct = default);
    Task<SiteSetting> UpsertAsync(string key, string value, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public class AdminSiteSettingService : IAdminSiteSettingService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly ILogger<AdminSiteSettingService> _logger;

    public AdminSiteSettingService(
        IDbContextFactory<InkrepublikDbContext> factory,
        ILogger<AdminSiteSettingService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<SiteSetting>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.SiteSettings
            .AsNoTracking()
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Key)
            .ToListAsync(ct);
    }

    public async Task<SiteSetting> UpsertAsync(string key, string value, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);

        if (existing is null)
        {
            existing = new SiteSetting
            {
                Key = key.Trim(),
                Value = value,
                UpdatedAt = DateTime.UtcNow,
            };
            db.SiteSettings.Add(existing);
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Site setting '{Key}' saved", key);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var setting = await db.SiteSettings.FindAsync([id], ct);
        if (setting is null) return false;

        db.SiteSettings.Remove(setting);
        await db.SaveChangesAsync(ct);
        return true;
    }
}