using Core.Models;
using Core.Models.Account;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[Authorize]
public class ManageModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserSessionService _userSessionService;

    public ManageModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserSessionService userSessionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userSessionService = userSessionService;
    }

    public ManageAccountViewModel Account { get; private set; } = new();
    public IReadOnlyCollection<UserSessionViewModel> Sessions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        await LoadAsync(user);
    }

    public async Task<IActionResult> OnPostSignOutEverywhereAsync()
    {
        var user = await GetCurrentUserAsync();
        await _userSessionService.RevokeAllUserSessionsAsync(
            user,
            "Пользователь запросил выход на всех устройствах.",
            user.Id);
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Account/Login");
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Account = new ManageAccountViewModel
        {
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled,
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user)
        };

        Sessions = await _userSessionService.GetUserSessionsAsync(
            user.Id,
            UserSessionService.GetCurrentSessionId(HttpContext),
            HttpContext.RequestAborted);
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user ?? throw new InvalidOperationException("Текущий пользователь не найден.");
    }
}
