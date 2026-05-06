using Core.Models;
using Core.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[Authorize]
public class ManageModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public ManageAccountViewModel Account { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        Account = new ManageAccountViewModel
        {
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user ?? throw new InvalidOperationException("Current user was not found.");
    }
}
