using System.Text;
using Core.Models;
using Core.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Core.Pages.Account;

[Authorize]
public class ChangeEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender<ApplicationUser> _emailSender;

    public ChangeEmailModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender<ApplicationUser> emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public ChangeEmailViewModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        Input.CurrentEmail = user.Email;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await GetCurrentUserAsync();
        Input.CurrentEmail = user.Email;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (string.Equals(user.Email, Input.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["StatusMessage"] = "Email is unchanged.";
            return RedirectToPage("/Account/Manage");
        }

        var code = EncodeToken(await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmailChange",
            pageHandler: null,
            values: new { userId = user.Id, email = Input.NewEmail, code },
            protocol: Request.Scheme);

        await _emailSender.SendConfirmationLinkAsync(user, Input.NewEmail, callbackUrl ?? string.Empty);

        TempData["StatusMessage"] = "Confirmation link was sent to the new email.";
        return RedirectToPage("/Account/Manage");
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user ?? throw new InvalidOperationException("Current user was not found.");
    }

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
}
