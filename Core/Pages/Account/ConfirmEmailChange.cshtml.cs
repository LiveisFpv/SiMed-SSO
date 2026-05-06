using System.Text;
using Core.Models;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Core.Pages.Account;

[Authorize]
public class ConfirmEmailChangeModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;

    public ConfirmEmailChangeModel(
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService)
    {
        _userManager = userManager;
        _userSessionService = userSessionService;
    }

    public bool Succeeded { get; private set; }

    public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(code))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangeEmailAsync(user, email, DecodeToken(code));
        if (result.Succeeded)
        {
            var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
            if (setUserNameResult.Succeeded)
            {
                await _userSessionService.RefreshSignInWithCurrentSessionAsync(HttpContext, user);
            }
        }

        Succeeded = result.Succeeded;
        return Page();
    }

    private static string DecodeToken(string token) =>
        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
}
