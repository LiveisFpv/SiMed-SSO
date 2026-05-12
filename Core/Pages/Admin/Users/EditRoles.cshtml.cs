using Core.Identity;
using Core.Models;
using Core.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin.Users;

[Authorize(Roles = ApplicationRoles.Admin)]
public class EditRolesModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public EditRolesModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public EditUserRolesViewModel Input { get; set; } = new() { UserId = string.Empty };

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        Input = await BuildEditRolesViewModelAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.UserId);
        if (user is null)
        {
            return NotFound();
        }

        var selectedRoles = Input.Roles
            .Where(role => role.IsSelected)
            .Select(role => role.RoleName)
            .Where(role => ApplicationRoles.All.Contains(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (user.Id == _userManager.GetUserId(User) && !selectedRoles.Contains(ApplicationRoles.Admin))
        {
            ModelState.AddModelError(string.Empty, "Нельзя убрать роль Admin у собственного аккаунта.");
            Input = await BuildEditRolesViewModelAsync(user, selectedRoles);
            return Page();
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
                Input = await BuildEditRolesViewModelAsync(user, selectedRoles);
                return Page();
            }
        }

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                AddIdentityErrors(removeResult);
                Input = await BuildEditRolesViewModelAsync(user, selectedRoles);
                return Page();
            }
        }

        await _userManager.UpdateSecurityStampAsync(user);

        TempData["StatusMessage"] = "Роли пользователя обновлены.";
        return RedirectToPage("./Details", new { id = user.Id });
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

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
