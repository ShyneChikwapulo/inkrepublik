namespace Inkrepublik.Domain.Entities;

/// <summary>
/// Editable site-wide content (address, hours, hero copy, socials).
/// Key/value so the owner can update from the admin panel without a deploy.
/// </summary>
public class SiteSetting
{
    public int Id { get; set; }

    /// <summary>Unique key, e.g. "studio.address" or "home.hero.headline".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Value. Long strings (hero copy) are fine here.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Optional human-readable description shown in admin.</summary>
    public string? Description { get; set; }

    /// <summary>Grouping for the admin UI, e.g. "Contact", "Home", "Social".</summary>
    public string? Group { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}