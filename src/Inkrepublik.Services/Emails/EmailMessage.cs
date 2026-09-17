namespace Inkrepublik.Services.Emails;

/// <summary>
/// A single email to send. Immutable by design — callers build a new one
/// for each send, which makes it trivially thread-safe.
/// </summary>
public record EmailMessage(
    string To,
    string ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? ReplyTo = null);