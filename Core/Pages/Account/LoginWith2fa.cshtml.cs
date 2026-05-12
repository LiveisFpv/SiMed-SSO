using Core.Models;
using Core.Models.Account;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[AllowAnonymous]
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userSessionService = userSessionService;
    }

    [BindProperty]
    public LoginWith2faViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !await CanCompleteTwoFactorAsync(user))
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

        Input.RememberMe = rememberMe;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !await CanCompleteTwoFactorAsync(user))
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var code = NormalizeCode(Input.TwoFactorCode);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);

        if (!isValid)
        {
            await RecordFailedTwoFactorAttemptAsync(user);
            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return Page();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (Input.RememberMachine)
        {
            await _signInManager.RememberTwoFactorClientAsync(user);
        }

        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        await _userSessionService.CreateSessionAndSignInAsync(HttpContext, user, Input.RememberMe);
        return RedirectToLocal(Input.ReturnUrl);
    }

    private async Task RecordFailedTwoFactorAttemptAsync(ApplicationUser user)
    {
        if (!await _userManager.GetLockoutEnabledAsync(user))
        {
            return;
        }

        await _userManager.AccessFailedAsync(user);
    }

    private async Task<bool> CanCompleteTwoFactorAsync(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            return false;
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            return false;
        }

        return !await _userManager.IsLockedOutAsync(user);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    private static string NormalizeCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
}
