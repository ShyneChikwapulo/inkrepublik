using Inkrepublik.Data;
using Inkrepublik.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Admin;

/// <summary>
/// Validates admin credentials against the AdminUsers table. This is the
/// only place passwords are verified — everything else trusts the cookie.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Returns the matching AdminUser if email + password are correct and
    /// the user is active. Returns null otherwise.
    ///
    /// Deliberately returns null for both "no such user" and "wrong password"
    /// — never tell the caller which was wrong (avoids user enumeration).
    /// </summary>
    Task<AdminUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default);

    /// <summary>Updates LastLoginAt when a user successfully logs in.</summary>
    Task RecordLoginAsync(int adminUserId, CancellationToken ct = default);
}

public class AdminAuthService : IAdminAuthService
{
    private readonly IDbContextFactory<InkrepublikDbContext> _factory;
    private readonly IPasswordHasherService _hasher;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(
        IDbContextFactory<InkrepublikDbContext> factory,
        IPasswordHasherService hasher,
        ILogger<AdminAuthService> logger)
    {
        _factory = factory;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<AdminUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        await using var db = await _factory.CreateDbContextAsync(ct);

        // Case-insensitive email comparison. SQL Server's default collation
        // is usually CI already, but being explicit protects against a
        // case-sensitive collation being set at the DB level.
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (user is null || !user.IsActive)
        {
            // Add a small artificial delay so timing attacks can't distinguish
            // "no such user" from "wrong password". Not perfect, but cheap.
            await Task.Delay(Random.Shared.Next(80, 160), ct);
            _logger.LogWarning("Failed admin login attempt for {Email} (no active user)", email);
            return null;
        }

        if (!_hasher.Verify(user.PasswordHash, password))
        {
            await Task.Delay(Random.Shared.Next(80, 160), ct);
            _logger.LogWarning("Failed admin login attempt for {Email} (wrong password)", email);
            return null;
        }

        _logger.LogInformation("Successful admin login for {Email}", email);
        return user;
    }

    public async Task RecordLoginAsync(int adminUserId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var user = await db.AdminUsers.FindAsync([adminUserId], ct);
        if (user is null) return;

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}