using Core.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Core.Services.Email;

public sealed class SmtpEmailSender : IApplicationEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(SmtpOptions options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = _options.Host ?? throw new InvalidOperationException("SMTP host is not configured.");
        var fromEmail = _options.FromEmail ?? throw new InvalidOperationException("SMTP sender email is not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlMessage
        }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, _options.Port, GetSecureSocketOptions(_options.Port));

            if (_options.HasCredentials)
            {
                var username = _options.Username ?? throw new InvalidOperationException("SMTP username is not configured.");
                var password = _options.Password ?? throw new InvalidOperationException("SMTP password is not configured.");

                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send email to {Email} with subject {Subject}.", email, subject);
            throw;
        }
    }

    private static SecureSocketOptions GetSecureSocketOptions(int port) =>
        port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
}
