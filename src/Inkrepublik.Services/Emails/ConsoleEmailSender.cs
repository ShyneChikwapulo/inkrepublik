using Microsoft.Extensions.Logging;

namespace Inkrepublik.Services.Emails;

/// <summary>
/// Development implementation. Writes the email to the logger so you can
/// see the full message (including magic links) in the terminal.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
        => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var divider = new string('═', 72);
        var replyTo = string.IsNullOrWhiteSpace(message.ReplyTo) ? "(none)" : message.ReplyTo;
        var htmlPreview = message.HtmlBody.Length > 500
            ? message.HtmlBody[..500] + "…"
            : message.HtmlBody;

        // Build the whole thing as one string, then log once. This avoids
        // structured-logging placeholder count issues and gives us full
        // control over the layout.
        var output = $"""
            {divider}
            📧  EMAIL (dev mode — not actually sent)
            {divider}
            To:       {message.ToName} <{message.To}>
            Reply-To: {replyTo}
            Subject:  {message.Subject}

            ── Text body ────────────────────────────────────────────────────
            {message.TextBody}

            ── HTML body (first 500 chars) ──────────────────────────────────
            {htmlPreview}
            {divider}
            """;

        _logger.LogInformation("{EmailOutput}", output);

        return Task.CompletedTask;
    }
}