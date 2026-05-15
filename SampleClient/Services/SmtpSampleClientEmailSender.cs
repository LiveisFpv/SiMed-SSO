using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SampleClient.Models;
using SampleClient.Options;

namespace SampleClient.Services;

public sealed class SmtpSampleClientEmailSender : ISampleClientEmailSender
{
    private readonly SampleClientSmtpOptions _options;
    private readonly ILogger<SmtpSampleClientEmailSender> _logger;

    public SmtpSampleClientEmailSender(
        SampleClientSmtpOptions options,
        ILogger<SmtpSampleClientEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(SampleApplicationUser user, string email, string confirmationLink)
    {
        return SendEmailAsync(
            email,
            "Подтверждение email в SampleClient",
            $"""
            <p>Для подтверждения email в SampleClient перейдите по ссылке:</p>
            <p><a href="{confirmationLink}">Подтвердить email</a></p>
            """);
    }

    public Task SendPasswordResetLinkAsync(SampleApplicationUser user, string email, string resetLink)
    {
        return SendEmailAsync(
            email,
            "Восстановление пароля в SampleClient",
            $"""
            <p>Для смены пароля в SampleClient перейдите по ссылке:</p>
            <p><a href="{resetLink}">Сменить пароль</a></p>
            """);
    }

    private async Task SendEmailAsync(string email, string subject, string htmlBody)
    {
        var host = _options.Host ?? throw new InvalidOperationException("SampleClient SMTP host is not configured.");
        var fromEmail = _options.FromEmail ?? throw new InvalidOperationException("SampleClient SMTP sender email is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, _options.Port, GetSecureSocketOptions(_options.Port));

            if (_options.HasCredentials)
            {
                var username = _options.Username ?? throw new InvalidOperationException("SampleClient SMTP username is not configured.");
                var password = _options.Password ?? throw new InvalidOperationException("SampleClient SMTP password is not configured.");

                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send SampleClient email to {Email} with subject {Subject}.", email, subject);
            throw;
        }
    }

    private static SecureSocketOptions GetSecureSocketOptions(int port) =>
        port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
}
