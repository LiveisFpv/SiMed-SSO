using System.Net;
using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Core.Services.Email;

public sealed class IdentityEmailSender :
    IEmailSender<ApplicationUser>
{
    private readonly IApplicationEmailSender _emailSender;

    public IdentityEmailSender(IApplicationEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var body = $"""
            <p>Please confirm your email address for SiMed SSO.</p>
            <p><a href="{WebUtility.HtmlEncode(confirmationLink)}">Confirm email</a></p>
            """;

        return _emailSender.SendEmailAsync(email, "Confirm your email", body);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var body = $"""
            <p>A password reset was requested for your SiMed SSO account.</p>
            <p><a href="{WebUtility.HtmlEncode(resetLink)}">Reset password</a></p>
            """;

        return _emailSender.SendEmailAsync(email, "Reset your password", body);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var body = $"""
            <p>A password reset code was requested for your SiMed SSO account.</p>
            <p><strong>{WebUtility.HtmlEncode(resetCode)}</strong></p>
            """;

        return _emailSender.SendEmailAsync(email, "Reset your password", body);
    }
}
