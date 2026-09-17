namespace Inkrepublik.Services.Emails;

/// <summary>
/// Abstract email delivery. Two implementations:
///   - ConsoleEmailSender (dev): prints to the console, doesn't send.
///   - SmtpEmailSender (prod): sends via SMTP.
/// The active implementation is chosen in Program.cs based on config.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}