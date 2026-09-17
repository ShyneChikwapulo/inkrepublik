namespace Inkrepublik.Services.Emails;

/// <summary>
/// SMTP configuration, bound from appsettings or environment variables.
/// Empty by default in development; required in production.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The "from" address on outgoing emails. Often equals Username, but
    /// not always — some providers require a verified sender address.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Inkrepublik Tattoo Studio";

    /// <summary>
    /// If true, smtp sends will throw instead of silently logging. Useful
    /// for verifying production config is correct.
    /// </summary>
    public bool Enabled { get; set; }
}