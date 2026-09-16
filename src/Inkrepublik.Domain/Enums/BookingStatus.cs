namespace Inkrepublik.Domain.Enums;

/// <summary>
/// Lifecycle of a booking request, from submission to completion.
/// </summary>
public enum BookingStatus
{
    /// <summary>Client submitted the request; awaiting owner decision.</summary>
    Pending = 0,

    /// <summary>Owner confirmed the appointment and notified the client.</summary>
    Confirmed = 1,

    /// <summary>Owner declined the request (date unavailable, out of scope, etc.).</summary>
    Declined = 2,

    /// <summary>Appointment happened; work is done.</summary>
    Completed = 3,

    /// <summary>Cancelled by either party before the appointment.</summary>
    Cancelled = 4,

    /// <summary>Client didn't show up.</summary>
    NoShow = 5
}