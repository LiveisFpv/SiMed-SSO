using Core.Models;
using Core.Models.Account;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;

    public ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService)
    {
        _userManager = userManager;
        _userSessionService = userSessionService;
    }

    [BindProperty]
    public ChangePasswordViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await GetCurrentUserAsync();
        var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return Page();
        }

        await _userSessionService.RefreshSignInWithCurrentSessionAsync(HttpContext, user);
        TempData["StatusMessage"] = "Password was changed.";
        return RedirectToPage("/Account/Manage");
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user ?? throw new InvalidOperationException("Current user was not found.");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
