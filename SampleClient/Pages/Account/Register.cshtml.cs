using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SampleClient.Models;
using SampleClient.Options;
using SampleClient.Services;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class RegisterModel : PageModel
{
    private readonly UserManager<SampleApplicationUser> _userManager;
    private readonly SignInManager<SampleApplicationUser> _signInManager;
    private readonly ISampleClientEmailSender _emailSender;
    private readonly SampleClientIdentityOptions _identityOptions;

    public RegisterModel(
        UserManager<SampleApplicationUser> userManager,
        SignInManager<SampleApplicationUser> signInManager,
        ISampleClientEmailSender emailSender,
        SampleClientIdentityOptions identityOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _identityOptions = identityOptions;
    }

    [BindProperty]
    public LocalRegisterViewModel Input { get; set; } = new();

    public void OnGet(string? returnUrl = null)
    {
        Input.ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new SampleApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName) ? Input.Email : Input.DisplayName.Trim(),
            EmailConfirmed = !_identityOptions.RequireEmailVerification,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return Page();
        }

        await SendConfirmationLinkAsync(user);

        if (_identityOptions.RequireEmailVerification)
        {
            return RedirectToPage("/Account/RegisterConfirmation", new { email = Input.Email });
        }

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(Input.ReturnUrl);
    }

    private async Task SendConfirmationLinkAsync(SampleApplicationUser user)
    {
        var code = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(await _userManager.GenerateEmailConfirmationTokenAsync(user)));

        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId = user.Id, code },
            protocol: Request.Scheme);

        await _emailSender.SendConfirmationLinkAsync(user, user.Email ?? string.Empty, callbackUrl ?? string.Empty);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Profile");
    }
}
