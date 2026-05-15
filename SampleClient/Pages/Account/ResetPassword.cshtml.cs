using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SampleClient.Models;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class ResetPasswordModel : PageModel
{
    private readonly UserManager<SampleApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<SampleApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public ResetPasswordViewModel Input { get; set; } = new();

    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("/Account/ForgotPassword");
        }

        Input.Code = code;
        Input.Email = email ?? string.Empty;
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
            return RedirectToPage("/Account/ResetPasswordConfirmation");
        }

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        var result = await _userManager.ResetPasswordAsync(user, decodedCode, Input.Password);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            return RedirectToPage("/Account/ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
