using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlantCare.Application.Calendar;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Calendar;

internal sealed class SmtpCalendarShareEmailSender(IOptions<EmailOptions> options,
    ILogger<SmtpCalendarShareEmailSender> logger) : ICalendarShareEmailSender
{
    public Task<bool> SendAsync(string ownerEmail, string recipientEmail, Guid shareId, CancellationToken cancellationToken = default) =>
        SendMessageAsync(ownerEmail, recipientEmail, $"/calendar?share={shareId:D}", false, cancellationToken);

    public Task<bool> SendInvitationAsync(string ownerEmail, string recipientEmail, string token, CancellationToken cancellationToken = default) =>
        SendMessageAsync(ownerEmail, recipientEmail, $"/calendar?invitation={Uri.EscapeDataString(token)}", true, cancellationToken);

    private async Task<bool> SendMessageAsync(string ownerEmail, string recipientEmail, string path, bool invitation, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsComplete) return false;
        var link = settings.ClientBaseUrl.TrimEnd('/') + path;
        var owner = HtmlEncoder.Default.Encode(ownerEmail);
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "A PlantCare calendar was shared with you",
            Body = $"<p>{owner} shared a read-only PlantCare calendar with you.</p>" +
                $"<p><a href=\"{HtmlEncoder.Default.Encode(link)}\">View shared calendar</a></p>" +
                (invitation ? "<p>Create a PlantCare account using this email address and confirm it, then open this invitation link again to accept. This link expires in 7 days and can be used once.</p>" :
                    "<p>Sign in with the PlantCare account for this email address.</p>") +
                "<p>The owner can revoke access at any time.</p>" +
                "<p>This email does not subscribe you to marketing or automatically add events to Google Calendar.</p>",
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.UseSsl };
        if (!string.IsNullOrWhiteSpace(settings.Username)) client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try { await client.SendMailAsync(message, timeout.Token); return true; }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or OperationCanceledException)
        {
            // Provider errors may contain addresses or credentials; record only type.
            logger.LogWarning("Calendar share email was not accepted: {FailureType}.", exception.GetType().Name);
            return false;
        }
    }
}
