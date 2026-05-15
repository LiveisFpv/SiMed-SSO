using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SampleClient.Models;
using SampleClient.Services;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<SampleApplicationUser> _userManager;
    private readonly ISampleClientEmailSender _emailSender;

    public ResendEmailConfirmationModel(
        UserManager<SampleApplicationUser> userManager,
        ISampleClientEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public ResendConfirmationViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            var code = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(await _userManager.GenerateEmailConfirmationTokenAsync(user)));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, code },
                protocol: Request.Scheme);

            await _emailSender.SendConfirmationLinkAsync(user, Input.Email, callbackUrl ?? string.Empty);
        }

        TempData["StatusMessage"] = "Если аккаунт существует и email не подтвержден, ссылка отправлена.";
        return RedirectToPage();
    }
}
