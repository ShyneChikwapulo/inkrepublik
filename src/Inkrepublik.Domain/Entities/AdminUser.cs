namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A studio staff member with access to the admin panel.
/// Kept minimal in v1 — one or two users, no roles.
/// </summary>
public class AdminUser
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Display name for the admin UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Password hash. We'll use ASP.NET Core's PasswordHasher in Phase 5,
    /// so this will store the PBKDF2 hash string, not the password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}