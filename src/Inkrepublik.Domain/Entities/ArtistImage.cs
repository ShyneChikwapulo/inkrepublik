namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A single portfolio image belonging to an artist.
/// </summary>
public class ArtistImage
{
    public int Id { get; set; }

    public int ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;

    /// <summary>Relative path, e.g. "/uploads/portfolio/zaakirah-001.jpg".</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>Optional caption (style, placement, etc.).</summary>
    public string? Caption { get; set; }

    /// <summary>Sort order within the artist's portfolio.</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}