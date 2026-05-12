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
public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;

    public LoginWithRecoveryCodeModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userSessionService = userSessionService;
    }

    [BindProperty]
    public LoginWithRecoveryCodeViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !await CanCompleteTwoFactorAsync(user))
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

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

        var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(
            user,
            NormalizeRecoveryCode(Input.RecoveryCode));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid recovery code.");
            return Page();
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        await _userSessionService.CreateSessionAndSignInAsync(HttpContext, user, isPersistent: false);
        return RedirectToLocal(Input.ReturnUrl);
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

    private static string NormalizeRecoveryCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal);
}
