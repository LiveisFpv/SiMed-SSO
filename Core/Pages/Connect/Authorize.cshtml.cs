using Core.Identity;
using Core.Models;
using Core.Services.OAuth;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Core.Pages.Connect;

[Authorize]
public class AuthorizeModel : PageModel
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OAuthClaimsPrincipalFactory _principalFactory;

    public AuthorizeModel(
        IOpenIddictApplicationManager applicationManager,
        UserManager<ApplicationUser> userManager,
        OAuthClaimsPrincipalFactory principalFactory)
    {
        _applicationManager = applicationManager;
        _userManager = userManager;
        _principalFactory = principalFactory;
    }

    public string ApplicationName { get; private set; } = string.Empty;
    public string? ClientId { get; private set; }
    public IReadOnlyCollection<string> Scopes { get; private set; } = [];
    public Dictionary<string, string> AuthorizationParameters { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var request = GetRequest();
        if (!await HasAllowedScopesAsync(request))
        {
            return ForbidWithOpenIddictError(
                Errors.InvalidScope,
                "The requested scope is not allowed for this client application.");
        }

        await LoadAsync(request);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string submit)
    {
        var request = GetRequest();
        if (!await HasAllowedScopesAsync(request))
        {
            return ForbidWithOpenIddictError(
                Errors.InvalidScope,
                "The requested scope is not allowed for this client application.");
        }

        if (string.Equals(submit, "Deny", StringComparison.Ordinal))
        {
            return ForbidWithOpenIddictError(
                Errors.AccessDenied,
                "The authorization request was denied by the user.");
        }

        if (!string.Equals(submit, "Accept", StringComparison.Ordinal))
        {
            await LoadAsync(request);
            ModelState.AddModelError(string.Empty, "Invalid consent action.");
            return Page();
        }

        var user = await _userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("Current user was not found.");

        if (!user.IsActive)
        {
            return ForbidWithOpenIddictError(
                Errors.LoginRequired,
                "The user is no longer allowed to sign in.");
        }

        var principal = await _principalFactory.CreateAsync(user, request.GetScopes());
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task LoadAsync(OpenIddictRequest request)
    {
        AuthorizationParameters = Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.Ordinal);

        ClientId = request.ClientId;
        Scopes = request.GetScopes().ToArray();

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty, HttpContext.RequestAborted);
        ApplicationName = application is null
            ? request.ClientId ?? "Unknown application"
            : await _applicationManager.GetDisplayNameAsync(application, HttpContext.RequestAborted) ?? request.ClientId ?? "Unknown application";
    }

    private async Task<bool> HasAllowedScopesAsync(OpenIddictRequest request)
    {
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty, HttpContext.RequestAborted);
        if (application is null)
        {
            return false;
        }

        var settings = await _applicationManager.GetSettingsAsync(application, HttpContext.RequestAborted);
        var configuredScopes = settings.TryGetValue(OAuthClientSettings.Scopes, out var value)
            ? value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal)
            : OAuthScopes.All.ToHashSet(StringComparer.Ordinal);

        return request.GetScopes().All(configuredScopes.Contains);
    }

    private OpenIddictRequest GetRequest()
    {
        return HttpContext.GetOpenIddictServerRequest()
               ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
    }

    private ForbidResult ForbidWithOpenIddictError(string error, string description)
    {
        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }
}
