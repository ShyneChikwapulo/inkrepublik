namespace Inkrepublik.Services.Emails;

/// <summary>
/// Resend API configuration. Simpler than SMTP — just an API key and sender.
/// </summary>
public class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>The Resend API key (starts with "re_").</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>From address — must be onboarding@resend.dev until a domain is verified.</summary>
    public string FromAddress { get; set; } = "onboarding@resend.dev";

    /// <summary>Display name shown in email clients.</summary>
    public string FromName { get; set; } = "Inkrepublik Tattoo Studio";
}