using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SampleClient.Authentication;
using SampleClient.Models;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class ExternalLoginModel : PageModel
{
    private readonly SignInManager<SampleApplicationUser> _signInManager;

    public ExternalLoginModel(SignInManager<SampleApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        if (!string.Equals(provider, SampleClientAuthenticationSchemes.SiMedSso, StringComparison.Ordinal))
        {
            return RedirectToPage("/Account/Login", new { returnUrl });
        }

        var redirectUrl = Url.Page("/Account/ExternalLoginCallback", values: new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }
}
