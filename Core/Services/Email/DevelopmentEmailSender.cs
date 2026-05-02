namespace Core.Services.Email;

public sealed class DevelopmentEmailSender : IApplicationEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation(
            "Development email sender intercepted message. To: {Email}; Subject: {Subject}; Body: {Body}",
            email,
            subject,
            htmlMessage);

        return Task.CompletedTask;
    }
}
