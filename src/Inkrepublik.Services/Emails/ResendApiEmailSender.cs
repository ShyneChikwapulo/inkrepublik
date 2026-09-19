using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Inkrepublik.Services.Emails;

/// <summary>
/// Sends emails via Resend's HTTPS API (port 443).
///
/// Why not SMTP: Render (and Railway, Heroku, etc.) block outbound SMTP
/// ports (25, 465, 587) on free/low tiers to prevent spam. The HTTPS API
/// uses port 443 — the same as any web request — and is never blocked.
/// </summary>
public class ResendApiEmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendApiEmailSender> _logger;

    public ResendApiEmailSender(
        IResend resend,
        IOptions<ResendOptions> options,
        ILogger<ResendApiEmailSender> logger)
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var email = new Resend.EmailMessage
        {
            From = string.IsNullOrWhiteSpace(_options.FromName)
                ? _options.FromAddress
                : $"{_options.FromName} <{_options.FromAddress}>",
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };

        // Assign collections to freshly-created lists. The SDK leaves these
        // properties null by default, so we can't append or reassign without
        // initializing them first.
        email.To.Add(message.To);

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            email.ReplyTo ??= new EmailAddressList();
            email.ReplyTo.Add(message.ReplyTo);
        }
        try
        {
            await _resend.EmailSendAsync(email, ct);
            _logger.LogInformation("Sent email via Resend API to {To}: {Subject}",
                message.To, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Resend API to {To}: {Subject}",
                message.To, message.Subject);
            throw;
        }
    }
}