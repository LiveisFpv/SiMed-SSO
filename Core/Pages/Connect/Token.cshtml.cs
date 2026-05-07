using Core.Models;
using Core.Services.OAuth;
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
public class TokenModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly OAuthClaimsPrincipalFactory _principalFactory;

    public TokenModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        OAuthClaimsPrincipalFactory principalFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _principalFactory = principalFactory;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            throw new InvalidOperationException("The specified grant type is not supported.");
        }

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var sourcePrincipal = result.Principal;
        if (sourcePrincipal is null)
        {
            return InvalidGrant("The token is no longer valid.");
        }

        var userId = sourcePrincipal.GetClaim(Claims.Subject);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return InvalidGrant("The token is no longer valid.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return InvalidGrant("The token is no longer valid.");
        }

        if (!user.IsActive || !await _signInManager.CanSignInAsync(user))
        {
            return InvalidGrant("The user is no longer allowed to sign in.");
        }

        var principal = await _principalFactory.CreateAsync(user, sourcePrincipal.GetScopes());
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private ForbidResult InvalidGrant(string description)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }
}
