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
            <p>Подтвердите email для аккаунта SiMed SSO.</p>
            <p><a href="{WebUtility.HtmlEncode(confirmationLink)}">Подтвердить email</a></p>
            """;

        return _emailSender.SendEmailAsync(email, "Подтверждение email", body);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var body = $"""
            <p>Запрошен сброс пароля для аккаунта SiMed SSO.</p>
            <p><a href="{WebUtility.HtmlEncode(resetLink)}">Сбросить пароль</a></p>
            """;

        return _emailSender.SendEmailAsync(email, "Сброс пароля", body);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var body = $"""
            <p>Запрошен код сброса пароля для аккаунта SiMed SSO.</p>
            <p><strong>{WebUtility.HtmlEncode(resetCode)}</strong></p>
            """;

        return _emailSender.SendEmailAsync(email, "Сброс пароля", body);
    }
}
