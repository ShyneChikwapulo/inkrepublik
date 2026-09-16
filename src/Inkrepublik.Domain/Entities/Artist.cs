namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A tattoo or piercing artist working at the studio.
/// </summary>
public class Artist
{
    public int Id { get; set; }

    /// <summary>Display name shown on the site.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short tagline, e.g. "Fine-line specialist".</summary>
    public string? Title { get; set; }

    /// <summary>Full bio, shown on the artist's detail page.</summary>
    public string? Bio { get; set; }

    /// <summary>Comma-separated list of specialties for quick display.</summary>
    public string? Specialties { get; set; }

    /// <summary>Relative path to profile photo, e.g. "/uploads/artists/zaakirah.jpg".</summary>
    public string? ProfileImagePath { get; set; }

    /// <summary>Instagram handle (without @).</summary>
    public string? InstagramHandle { get; set; }

    /// <summary>Whether this artist is currently taking bookings and shown publicly.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Manual sort order for public display (lower = earlier).</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ArtistImage> PortfolioImages { get; set; } = new List<ArtistImage>();
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
}