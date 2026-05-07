using Core.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Core.Pages.Connect;

[IgnoreAntiforgeryToken]
public class UserInfoModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UserInfoModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await HandleAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await HandleAsync();
    }

    private async Task<IActionResult> HandleAsync()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;
        if (principal is null)
        {
            return InvalidToken("The access token is missing or invalid.");
        }

        var userId = principal.GetClaim(Claims.Subject);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return InvalidToken("The access token is missing a subject claim.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return InvalidToken("The user no longer exists.");
        }

        if (!user.IsActive || !await _signInManager.CanSignInAsync(user))
        {
            return InvalidToken("The user is no longer allowed to sign in.");
        }

        var response = new Dictionary<string, object?>
        {
            [Claims.Subject] = user.Id
        };

        if (principal.HasScope(Scopes.Profile))
        {
            response[Claims.Name] = user.UserName;
            response[Claims.PreferredUsername] = user.UserName;
        }

        if (principal.HasScope(Scopes.Email))
        {
            response[Claims.Email] = user.Email;
            response[Claims.EmailVerified] = user.EmailConfirmed;
        }

        return new JsonResult(response);
    }

    private ForbidResult InvalidToken(string description)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }
}
