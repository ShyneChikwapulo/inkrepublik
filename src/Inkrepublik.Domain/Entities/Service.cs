using Inkrepublik.Domain.Enums;

namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A service the studio offers (tattoo session, piercing, jewelry change, consultation).
/// </summary>
public class Service
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ServiceCategory Category { get; set; }

    /// <summary>
    /// Price "from" in ZAR. Null means "enquire" (e.g. custom tattoos).
    /// </summary>
    public decimal? PriceFrom { get; set; }

    /// <summary>
    /// Optional upper bound. Null with PriceFrom set means "from X upwards".
    /// </summary>
    public decimal? PriceTo { get; set; }

    /// <summary>
    /// Typical duration in minutes. Used as a hint on the booking form.
    /// </summary>
    public int? TypicalDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
}