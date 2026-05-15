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
public sealed class ForgotPasswordModel : PageModel
{
    private readonly UserManager<SampleApplicationUser> _userManager;
    private readonly ISampleClientEmailSender _emailSender;

    public ForgotPasswordModel(
        UserManager<SampleApplicationUser> userManager,
        ISampleClientEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public ForgotPasswordViewModel Input { get; set; } = new();

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
        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            var code = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(await _userManager.GeneratePasswordResetTokenAsync(user)));

            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { code, email = Input.Email },
                protocol: Request.Scheme);

            await _emailSender.SendPasswordResetLinkAsync(user, Input.Email, resetLink ?? string.Empty);
        }

        return RedirectToPage("/Account/ForgotPasswordConfirmation");
    }
}
