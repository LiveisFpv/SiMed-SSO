using SampleClient.Models;

namespace SampleClient.Services;

public interface ISampleClientEmailSender
{
    Task SendConfirmationLinkAsync(SampleApplicationUser user, string email, string confirmationLink);
    Task SendPasswordResetLinkAsync(SampleApplicationUser user, string email, string resetLink);
}
