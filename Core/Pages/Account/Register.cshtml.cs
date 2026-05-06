using System.Text;
using Core.Identity;
using Core.Models;
using Core.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Core.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender<ApplicationUser> _emailSender;
    private readonly IdentityOptions _identityOptions;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender<ApplicationUser> emailSender,
        IOptions<IdentityOptions> identityOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _identityOptions = identityOptions.Value;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

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

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return Page();
        }

        await _userManager.AddToRoleAsync(user, ApplicationRoles.User);
        await SendEmailConfirmationAsync(user, Input.Email);

        if (_identityOptions.SignIn.RequireConfirmedAccount)
        {
            return RedirectToPage("/Account/RegisterConfirmation", new { email = Input.Email });
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(Input.ReturnUrl);
    }

    private async Task SendEmailConfirmationAsync(ApplicationUser user, string email)
    {
        var code = EncodeToken(await _userManager.GenerateEmailConfirmationTokenAsync(user));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId = user.Id, code },
            protocol: Request.Scheme);

        await _emailSender.SendConfirmationLinkAsync(user, email, callbackUrl ?? string.Empty);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
}
