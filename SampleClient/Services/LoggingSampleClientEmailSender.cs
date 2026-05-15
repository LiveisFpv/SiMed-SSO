using SampleClient.Models;

namespace SampleClient.Services;

public sealed class LoggingSampleClientEmailSender : ISampleClientEmailSender
{
    private readonly ILogger<LoggingSampleClientEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;

    public LoggingSampleClientEmailSender(
        ILogger<LoggingSampleClientEmailSender> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task SendConfirmationLinkAsync(SampleApplicationUser user, string email, string confirmationLink)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "SampleClient email confirmation for {Email}: {ConfirmationLink}",
                email,
                confirmationLink);
        }
        else
        {
            _logger.LogWarning(
                "SampleClient SMTP is not implemented in V1. Confirmation link for {Email}: {ConfirmationLink}",
                email,
                confirmationLink);
        }

        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(SampleApplicationUser user, string email, string resetLink)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "SampleClient password reset for {Email}: {ResetLink}",
                email,
                resetLink);
        }
        else
        {
            _logger.LogWarning(
                "SampleClient SMTP is not implemented in V1. Password reset link for {Email}: {ResetLink}",
                email,
                resetLink);
        }

        return Task.CompletedTask;
    }
}
