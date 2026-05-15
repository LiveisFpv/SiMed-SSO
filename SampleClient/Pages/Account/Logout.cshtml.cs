using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SampleClient.Models;

namespace SampleClient.Pages.Account;

[Authorize]
public sealed class LogoutModel : PageModel
{
    private readonly SignInManager<SampleApplicationUser> _signInManager;

    public LogoutModel(SignInManager<SampleApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
