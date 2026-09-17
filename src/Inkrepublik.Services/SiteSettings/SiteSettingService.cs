using Inkrepublik.Data;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.SiteSettings;

/// <summary>
/// Read-side queries for site settings (key/value pairs the owner edits).
/// </summary>
public interface ISiteSettingService
{
    Task<string> GetAsync(string key, string fallback = "", CancellationToken ct = default);

    Task<Dictionary<string, string>> GetByGroupAsync(string group, CancellationToken ct = default);
}

public class SiteSettingService : ISiteSettingService
{
    private readonly InkrepublikDbContext _db;

    public SiteSettingService(InkrepublikDbContext db) => _db = db;

    public async Task<string> GetAsync(string key, string fallback = "", CancellationToken ct = default)
    {
        var setting = await _db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        return setting?.Value ?? fallback;
    }

    public async Task<Dictionary<string, string>> GetByGroupAsync(string group, CancellationToken ct = default)
    {
        return await _db.SiteSettings
            .AsNoTracking()
            .Where(s => s.Group == group)
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
    }
}