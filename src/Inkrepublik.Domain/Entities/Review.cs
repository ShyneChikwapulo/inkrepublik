namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A client review/testimonial shown on the site.
/// </summary>
public class Review
{
    public int Id { get; set; }

    public string ClientName { get; set; } = string.Empty;

    /// <summary>1–5 stars.</summary>
    public int Rating { get; set; } = 5;

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Where the review came from, e.g. "Google", "Facebook", "Walk-in".
    /// Optional — for our own record-keeping.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>Date of the review (may differ from CreatedAt).</summary>
    public DateTime? ReviewedOn { get; set; }

    /// <summary>Only approved reviews are shown publicly.</summary>
    public bool IsApproved { get; set; }

    /// <summary>Featured reviews get highlighted on the home page.</summary>
    public bool IsFeatured { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}