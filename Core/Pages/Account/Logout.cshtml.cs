using Core.Models;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserSessionService _userSessionService;

    public LogoutModel(
        SignInManager<ApplicationUser> signInManager,
        IUserSessionService userSessionService)
    {
        _signInManager = signInManager;
        _userSessionService = userSessionService;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _userSessionService.RevokeCurrentSessionAsync(HttpContext, "Logout");
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
