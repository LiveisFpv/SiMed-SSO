using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SampleClient.Models;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class ExternalLoginCallbackModel : PageModel
{
    private readonly SignInManager<SampleApplicationUser> _signInManager;
    private readonly UserManager<SampleApplicationUser> _userManager;

    public ExternalLoginCallbackModel(
        SignInManager<SampleApplicationUser> signInManager,
        UserManager<SampleApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public string StatusMessage { get; private set; } = "Не удалось выполнить вход через SiMed SSO.";

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            StatusMessage = "SiMed SSO отклонил вход.";
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            StatusMessage = "Не удалось получить данные входа от SiMed SSO.";
            return Page();
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var linkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (linkedUser is not null)
            {
                await UpdateSuccessfulLoginAsync(linkedUser, info);
            }

            return RedirectToLocal(returnUrl);
        }

        var user = await ResolveOrCreateUserAsync(info);
        if (user is null)
        {
            return Page();
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            var alreadyLinkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (alreadyLinkedUser is null || !string.Equals(alreadyLinkedUser.Id, user.Id, StringComparison.Ordinal))
            {
                StatusMessage = "Не удалось связать локальный аккаунт с SiMed SSO.";
                return Page();
            }
        }

        await UpdateSuccessfulLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return RedirectToLocal(returnUrl);
    }

    private async Task<SampleApplicationUser?> ResolveOrCreateUserAsync(ExternalLoginInfo info)
    {
        var subject = info.Principal.FindFirstValue("sub") ?? info.ProviderKey;
        var email = info.Principal.FindFirstValue("email");
        var emailVerified = string.Equals(
            info.Principal.FindFirstValue("email_verified"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(email))
        {
            StatusMessage = "SiMed SSO не вернул email пользователя.";
            return null;
        }

        var existingBySubject = await _userManager.Users
            .FirstOrDefaultAsync(user => user.SsoSubject == subject);
        if (existingBySubject is not null)
        {
            return existingBySubject;
        }

        var existingByEmail = await _userManager.FindByEmailAsync(email);
        if (existingByEmail is not null)
        {
            if (!emailVerified)
            {
                StatusMessage = "Не удалось безопасно связать аккаунт с SiMed SSO.";
                return null;
            }

            existingByEmail.SsoSubject = subject;
            if (string.IsNullOrWhiteSpace(existingByEmail.DisplayName))
            {
                existingByEmail.DisplayName = GetDisplayName(info, email);
            }

            if (emailVerified && !existingByEmail.EmailConfirmed)
            {
                existingByEmail.EmailConfirmed = true;
            }

            await _userManager.UpdateAsync(existingByEmail);
            return existingByEmail;
        }

        var user = new SampleApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailVerified,
            DisplayName = GetDisplayName(info, email),
            SsoSubject = subject,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            return user;
        }

        StatusMessage = string.Join(" ", createResult.Errors.Select(error => error.Description));
        return null;
    }

    private async Task UpdateSuccessfulLoginAsync(SampleApplicationUser user, ExternalLoginInfo info)
    {
        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.SsoSubject ??= info.Principal.FindFirstValue("sub") ?? info.ProviderKey;
        await _userManager.UpdateAsync(user);

        foreach (var token in info.AuthenticationTokens ?? [])
        {
            if (!string.IsNullOrWhiteSpace(token.Name) && token.Value is not null)
            {
                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    info.LoginProvider,
                    token.Name,
                    token.Value);
            }
        }
    }

    private static string GetDisplayName(ExternalLoginInfo info, string email)
    {
        return info.Principal.FindFirstValue("name") ??
               info.Principal.FindFirstValue("preferred_username") ??
               email;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Profile");
    }
}
