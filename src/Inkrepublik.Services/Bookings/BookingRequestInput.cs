using System.ComponentModel.DataAnnotations;

namespace Inkrepublik.Services.Bookings;

/// <summary>
/// What the client submits through the booking form. Deliberately a separate
/// shape from BookingRequest — the entity has fields (Id, MagicToken, Status,
/// timestamps) that come from the server, not the client.
/// </summary>
public class BookingRequestInput
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(120)]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(200)]
    public string ClientEmail { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(40)]
    public string? ClientPhone { get; set; }

    /// <summary>Null = "no preference".</summary>
    public int? ArtistId { get; set; }

    /// <summary>Null = "not sure yet".</summary>
    public int? ServiceId { get; set; }

    [Required(ErrorMessage = "Please pick a date.")]
    public DateOnly PreferredDate { get; set; }

    [Required(ErrorMessage = "Please pick a time.")]
    public TimeOnly PreferredTime { get; set; }

    [Required(ErrorMessage = "Please tell us about your idea.")]
    [StringLength(4000, MinimumLength = 10,
        ErrorMessage = "Please tell us a bit more — at least 10 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Combine date + time into a single DateTime.</summary>
    public DateTime PreferredDateTime => PreferredDate.ToDateTime(PreferredTime);
}