using Inkrepublik.Data;
using Microsoft.EntityFrameworkCore;

namespace Inkrepublik.Services.SiteSettings;

public interface ISiteSettingService
{
    Task<string> GetAsync(string key, string fallback = "", CancellationToken ct = default);
    Task<Dictionary<string, string>> GetByGroupAsync(string group, CancellationToken ct = default);
}

public class SiteSettingService : ISiteSettingService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;

    public SiteSettingService(IDbContextFactory<InkrepublikDbContext> factory)
        => _factory = factory;

    public async Task<string> GetAsync(string key, string fallback = "", CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var setting = await db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        return setting?.Value ?? fallback;
    }

    public async Task<Dictionary<string, string>> GetByGroupAsync(string group, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.SiteSettings
            .AsNoTracking()
            .Where(s => s.Group == group)
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
    }
}