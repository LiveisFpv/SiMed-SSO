using Core.Identity;
using Core.Models;
using Core.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Core.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Users()
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

        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(await BuildDetailsViewModelAsync(user));
    }

    public async Task<IActionResult> EditRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(await BuildEditRolesViewModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditUserRolesViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return NotFound();
        }

        var selectedRoles = model.Roles
            .Where(role => role.IsSelected)
            .Select(role => role.RoleName)
            .Where(role => ApplicationRoles.All.Contains(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (user.Id == _userManager.GetUserId(User) && !selectedRoles.Contains(ApplicationRoles.Admin))
        {
            ModelState.AddModelError(string.Empty, "You cannot remove the Admin role from your own account.");
            return View(await BuildEditRolesViewModelAsync(user, selectedRoles));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var managedCurrentRoles = currentRoles
            .Where(role => ApplicationRoles.All.Contains(role))
            .ToArray();

        var rolesToAdd = selectedRoles.Except(managedCurrentRoles).ToArray();
        var rolesToRemove = managedCurrentRoles.Except(selectedRoles).ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                AddIdentityErrors(addResult);
                return View(await BuildEditRolesViewModelAsync(user, selectedRoles));
            }
        }

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                AddIdentityErrors(removeResult);
                return View(await BuildEditRolesViewModelAsync(user, selectedRoles));
            }
        }

        await _userManager.UpdateSecurityStampAsync(user);

        TempData["StatusMessage"] = "User roles were updated.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        user.IsActive = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(updateResult);
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        await _userManager.UpdateSecurityStampAsync(user);

        TempData["StatusMessage"] = "User was deactivated.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string id)
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

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(updateResult);
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        await _userManager.UpdateSecurityStampAsync(user);

        TempData["StatusMessage"] = "User was reactivated.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
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
            Roles = (await _userManager.GetRolesAsync(user)).ToArray()
        };
    }

    private async Task<EditUserRolesViewModel> BuildEditRolesViewModelAsync(
        ApplicationUser user,
        IReadOnlyCollection<string>? selectedRoles = null)
    {
        selectedRoles ??= (await _userManager.GetRolesAsync(user)).ToArray();

        return new EditUserRolesViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = ApplicationRoles.All
                .Select(role => new RoleSelectionViewModel
                {
                    RoleName = role,
                    IsSelected = selectedRoles.Contains(role)
                })
                .ToList()
        };
    }

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
