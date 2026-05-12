using Core.Identity;
using Core.Models;
using Core.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Core.Pages.Admin.Users;

[Authorize(Roles = ApplicationRoles.Admin)]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IReadOnlyCollection<AdminUserListItemViewModel> Users { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();
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
            TempData["ErrorMessage"] = "Нельзя деактивировать собственный аккаунт.";
            return RedirectToPage();
        }

        user.IsActive = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(result);
            return RedirectToPage();
        }

        await _userManager.UpdateSecurityStampAsync(user);
        TempData["StatusMessage"] = "Пользователь деактивирован.";
        return RedirectToPage();
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
            return RedirectToPage();
        }

        await _userManager.UpdateSecurityStampAsync(user);
        TempData["StatusMessage"] = "Пользователь реактивирован.";
        return RedirectToPage();
    }

    private async Task LoadUsersAsync()
    {
        var currentUserId = _userManager.GetUserId(User);
        var users = await _userManager.Users
            .OrderByDescending(user => user.CreatedAtUtc)
            .ToListAsync();

        var model = new List<AdminUserListItemViewModel>();

        foreach (var user in users)
        {
            model.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                IsCurrentUser = user.Id == currentUserId,
                IsLockedOut = IsLockedOut(user),
                LockoutEnd = user.LockoutEnd,
                CreatedAtUtc = user.CreatedAtUtc,
                Roles = (await _userManager.GetRolesAsync(user)).ToArray()
            });
        }

        Users = model;
    }

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
