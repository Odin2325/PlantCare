using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PlantCare.Infrastructure.Identity;

internal sealed class SmtpIdentityEmailSender(
    IOptions<EmailOptions> options,
    IWebHostEnvironment environment,
    ILogger<SmtpIdentityEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    private readonly EmailOptions options = options.Value;

    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink) =>
        SendAsync(
            email,
            "Confirm your PlantCare account",
            $"Confirm your email address by opening " +
            $"<a href=\"{HtmlEncoder.Default.Encode(confirmationLink)}\">this secure link</a>.",
            confirmationLink);

    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink) =>
        SendAsync(
            email,
            "Reset your PlantCare password",
            $"Reset your password by opening " +
            $"<a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">this secure link</a>.",
            resetLink);

    public Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        var resetLink = BuildResetLink(email, resetCode);

        return SendAsync(
            email,
            "Reset your PlantCare password",
            $"Reset your password by opening " +
            $"<a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">this secure link</a>.",
            resetLink);
    }

    private string BuildResetLink(
        string email,
        string resetCode)
    {
        var query =
            $"email={Uri.EscapeDataString(email)}" +
            $"&code={Uri.EscapeDataString(resetCode)}";

        return
            $"{options.ClientBaseUrl.TrimEnd('/')}/reset-password?{query}";
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        string developmentLink)
    {
        if (!options.IsComplete)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Email delivery is not configured.");
            }

            logger.LogInformation(
                "Development email to {Recipient}: {Subject}. Link: {Link}",
                recipient,
                subject,
                developmentLink);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                options.FromAddress,
                options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(
            options.Host,
            options.Port)
        {
            EnableSsl = options.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            client.Credentials = new NetworkCredential(
                options.Username,
                options.Password);
        }

        await client.SendMailAsync(message);
    }
}
