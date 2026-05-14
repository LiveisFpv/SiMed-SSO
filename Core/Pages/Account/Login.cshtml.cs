using Core.Identity;
using Core.Models;
using Core.Models.Account;
using Core.Services.Mfa;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly ApplicationSignInManager _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;
    private readonly IMfaMethodService _mfaMethodService;

    public LoginModel(
        ApplicationSignInManager signInManager,
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService,
        IMfaMethodService mfaMethodService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userSessionService = userSessionService;
        _mfaMethodService = mfaMethodService;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    public void OnGet(string? returnUrl = null)
    {
        Input.ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
            return Page();
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var twoFactorProviders = await GetEnabledTwoFactorProvidersAsync(user);
            if (twoFactorProviders.Count > 0)
            {
                var twoFactorResult = await _signInManager.InitiateTwoFactorSignInAsync(user, Input.RememberMe);
                if (twoFactorResult.RequiresTwoFactor)
                {
                    return RedirectToPage(
                        "/Account/LoginWith2fa",
                        new
                        {
                            returnUrl = Input.ReturnUrl,
                            rememberMe = Input.RememberMe,
                            provider = twoFactorProviders.Count == 1 ? twoFactorProviders.Single() : null
                        });
                }
            }

            await _userSessionService.CreateSessionAndSignInAsync(HttpContext, user, Input.RememberMe);
            return RedirectToLocal(Input.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
        return Page();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    private async Task<IReadOnlyCollection<string>> GetEnabledTwoFactorProvidersAsync(ApplicationUser user)
    {
        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return [];
        }

        if (await _signInManager.IsTwoFactorClientRememberedAsync(user))
        {
            return [];
        }

        return await _mfaMethodService.GetEnabledProvidersAsync(user);
    }
}
