using Inkrepublik.Domain.Entities;
using Inkrepublik.Domain.Enums;
using Inkrepublik.Services.Emails;
using System.Net;

namespace Inkrepublik.Services.Bookings;

/// <summary>
/// Static builders for booking-related emails. HTML + plain text, both
/// generated from the same data so they stay in sync.
/// </summary>
public static class BookingEmailTemplates
{
    public static EmailMessage OwnerNotification(BookingRequest booking, string statusUrl)
    {
        var subject = $"New booking request — {booking.ClientName} ({booking.PreferredDateTime:ddd d MMM, HH:mm})";

        var text = $"""
            New booking request via the website.

            Client:    {booking.ClientName}
            Email:     {booking.ClientEmail}
            Phone:     {booking.ClientPhone ?? "(not provided)"}
            Preferred: {booking.PreferredDateTime:dddd, d MMMM yyyy 'at' HH:mm}
            Status:    {booking.Status}

            Description:
            {booking.Description}

            Manage this booking:
            {statusUrl}
            """;

        var html = BuildHtml(
            title: "New booking request",
            body: $"""
                <p><strong>{Esc(booking.ClientName)}</strong> has requested an appointment.</p>
                <table style="width:100%; border-collapse:collapse; margin: 1.5rem 0;">
                    {Row("Preferred date & time", $"{booking.PreferredDateTime:dddd, d MMMM yyyy 'at' HH:mm}")}
                    {Row("Email", Esc(booking.ClientEmail))}
                    {Row("Phone", Esc(booking.ClientPhone ?? "(not provided)"))}
                </table>
                <p style="margin: 1.5rem 0 0.5rem;"><strong>What they want:</strong></p>
                <p style="white-space:pre-wrap; padding:1rem; background:#f5f5f5; border-left:3px solid #8B1A1A;">{Esc(booking.Description)}</p>
                <p style="margin-top:2rem;">
                    <a href="{statusUrl}" style="display:inline-block; padding:0.75rem 1.5rem; background:#8B1A1A; color:#fff; text-decoration:none;">View booking details</a>
                </p>
                """);

        return new EmailMessage(
            To: "",
            ToName: "",
            Subject: subject,
            HtmlBody: html,
            TextBody: text,
            ReplyTo: booking.ClientEmail);
    }

    public static EmailMessage ClientConfirmation(BookingRequest booking, string statusUrl)
    {
        var subject = "We've received your booking request — Inkrepublik Tattoo Studio";

        var text = $"""
            Hi {booking.ClientName},

            Thank you for your booking request. We've received it and will be
            in touch within 24 hours to confirm the details.

            Your requested appointment:
              {booking.PreferredDateTime:dddd, d MMMM yyyy 'at' HH:mm}

            You can view or cancel your request at any time using this link:
            {statusUrl}

            This link is valid for 30 days.

            Description you provided:
            {booking.Description}

            — Inkrepublik Tattoo Studio
            Hout Bay, Cape Town
            """;

        var html = BuildHtml(
            title: $"Thanks, {Esc(booking.ClientName.Split(' ')[0])}",
            body: $"""
                <p>We've received your booking request and will be in touch within 24 hours to confirm the details.</p>

                <div style="margin: 1.5rem 0; padding: 1.25rem; background: #fdf6ec; border-left: 3px solid #c8b88a;">
                    <p style="margin:0; font-size:0.85rem; color:#888; text-transform:uppercase; letter-spacing:0.15em;">Requested appointment</p>
                    <p style="margin:0.4rem 0 0; font-size:1.1rem;">{booking.PreferredDateTime:dddd, d MMMM yyyy 'at' HH:mm}</p>
                </div>

                <p style="margin: 1.5rem 0 0.5rem;"><strong>What you told us:</strong></p>
                <p style="white-space:pre-wrap; padding:1rem; background:#f5f5f5; border-left:3px solid #8B1A1A;">{Esc(booking.Description)}</p>

                <p style="margin-top:2rem;">
                    <a href="{statusUrl}" style="display:inline-block; padding:0.75rem 1.5rem; background:#8B1A1A; color:#fff; text-decoration:none;">View or cancel this request</a>
                </p>

                <p style="margin-top: 2rem; font-size: 0.85rem; color: #888;">
                    This link is valid for 30 days. If you didn't make this request, you can ignore this email.
                </p>
                """);

        return new EmailMessage(
            To: booking.ClientEmail,
            ToName: booking.ClientName,
            Subject: subject,
            HtmlBody: html,
            TextBody: text);
    }

    // ----------------------------------------------------------------
    // Shared HTML shell
    // ----------------------------------------------------------------
    private static string BuildHtml(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8" /></head>
        <body style="margin:0; padding:2rem 1rem; background:#0a0a0a; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color:#2b2b2b;">
            <div style="max-width: 600px; margin: 0 auto; background:#ffffff; border-top: 4px solid #8B1A1A;">
                <div style="padding: 2rem;">
                    <h1 style="font-family: Georgia, serif; font-size: 1.5rem; color:#0a0a0a; margin: 0 0 1.5rem; letter-spacing:0.05em; text-transform: uppercase;">{title}</h1>
                    {body}
                </div>
                <div style="padding: 1rem 2rem; background:#f5f5f5; font-size:0.8rem; color:#888; text-align:center;">
                    Inkrepublik Tattoo Studio · Hout Bay, Cape Town
                </div>
            </div>
        </body>
        </html>
        """;

    private static string Row(string label, string value) => $"""
        <tr>
            <td style="padding: 0.4rem 0; font-weight:600; color:#555; width:40%;">{label}</td>
            <td style="padding: 0.4rem 0; color:#0a0a0a;">{value}</td>
        </tr>
        """;

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
}