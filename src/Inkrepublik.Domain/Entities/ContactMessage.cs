namespace Inkrepublik.Domain.Entities;

/// <summary>
/// A general enquiry submitted via the contact form (not a booking).
/// </summary>
public class ContactMessage
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Owner has read and dealt with this message.</summary>
    public bool IsHandled { get; set; }

    /// <summary>Owner's private notes about the message.</summary>
    public string? OwnerNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}