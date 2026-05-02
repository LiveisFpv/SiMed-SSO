namespace Core.Services.Email;

public interface IApplicationEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
