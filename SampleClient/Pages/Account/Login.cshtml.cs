using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SampleClient.Models;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    private readonly SignInManager<SampleApplicationUser> _signInManager;
    private readonly UserManager<SampleApplicationUser> _userManager;

    public LoginModel(
        SignInManager<SampleApplicationUser> signInManager,
        UserManager<SampleApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public LocalLoginViewModel Input { get; set; } = new();

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        Input.ReturnUrl = returnUrl;
        return Page();
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
            AddGenericError();
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAtUtc = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            return RedirectToLocal(Input.ReturnUrl);
        }

        AddGenericError();
        return Page();
    }

    private void AddGenericError()
    {
        ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
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
