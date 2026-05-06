using Core.Identity;
using Core.Models;
using Core.Models.Admin;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin.Users;

[Authorize(Roles = ApplicationRoles.Admin)]
public class DetailsModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;

    public DetailsModel(
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService)
    {
        _userManager = userManager;
        _userSessionService = userSessionService;
    }

    public AdminUserDetailsViewModel UserDetails { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        UserDetails = await BuildDetailsViewModelAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToPage(new { id = user.Id });
        }

        user.IsActive = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(result);
            return RedirectToPage(new { id = user.Id });
        }

        await _userSessionService.RevokeAllUserSessionsAsync(
            user,
            "User was deactivated.",
            _userManager.GetUserId(User));
        TempData["StatusMessage"] = "User was deactivated.";
        return RedirectToPage(new { id = user.Id });
    }

    public async Task<IActionResult> OnPostReactivateAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = true;
        user.LockoutEnabled = true;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(result);
            return RedirectToPage(new { id = user.Id });
        }

        await _userManager.UpdateSecurityStampAsync(user);
        TempData["StatusMessage"] = "User was reactivated.";
        return RedirectToPage(new { id = user.Id });
    }

    public async Task<IActionResult> OnPostRevokeSessionsAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userSessionService.RevokeAllUserSessionsAsync(
            user,
            "Administrator revoked all sessions.",
            _userManager.GetUserId(User));

        TempData["StatusMessage"] = "User sessions were revoked.";
        return RedirectToPage(new { id = user.Id });
    }

    private async Task<AdminUserDetailsViewModel> BuildDetailsViewModelAsync(ApplicationUser user)
    {
        return new AdminUserDetailsViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            IsActive = user.IsActive,
            IsCurrentUser = user.Id == _userManager.GetUserId(User),
            IsLockedOut = IsLockedOut(user),
            LockoutEnd = user.LockoutEnd,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            TwoFactorEnabled = user.TwoFactorEnabled,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = (await _userManager.GetRolesAsync(user)).ToArray(),
            Sessions = await _userSessionService.GetUserSessionsAsync(
                user.Id,
                UserSessionService.GetCurrentSessionId(HttpContext),
                HttpContext.RequestAborted)
        };
    }

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
