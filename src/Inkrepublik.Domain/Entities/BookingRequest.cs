using Inkrepublik.Domain.Enums;

namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A client's request for an appointment. Confirmed manually by the owner.
/// </summary>
public class BookingRequest
{
    public int Id { get; set; }

    // --- Client details ---
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }

    // --- What they want ---
    /// <summary>Null means "any artist" — client has no preference.</summary>
    public int? ArtistId { get; set; }
    public Artist? Artist { get; set; }

    /// <summary>Null means the client isn't sure yet.</summary>
    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    /// <summary>
    /// Preferred date and time for the appointment (free-form request).
    /// Owner may reschedule on confirm.
    /// </summary>
    public DateTime PreferredDateTime { get; set; }

    /// <summary>Description of what they want — placement, size, reference notes.</summary>
    public string? Description { get; set; }

    // --- Lifecycle ---
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>When the owner actually confirmed the appointment (may differ from PreferredDateTime).</summary>
    public DateTime? ConfirmedDateTime { get; set; }

    /// <summary>Internal notes from the owner (not shown to client).</summary>
    public string? OwnerNotes { get; set; }

    /// <summary>
    /// Opaque token used in the client's magic link to view/manage this booking.
    /// </summary>
    public string MagicToken { get; set; } = string.Empty;

    /// <summary>
    /// When the magic link stops working. Regenerable by the owner.
    /// </summary>
    public DateTime MagicTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}