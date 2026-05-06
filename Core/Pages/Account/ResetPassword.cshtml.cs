using System.Text;
using Core.Models;
using Core.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Core.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public ResetPasswordViewModel Input { get; set; } = new();

    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (code is null)
        {
            return BadRequest();
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

        var result = await _userManager.ResetPasswordAsync(user, DecodeToken(Input.Code), Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage("/Account/ResetPasswordConfirmation");
        }

        AddIdentityErrors(result);
        return Page();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string DecodeToken(string token) =>
        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
}
