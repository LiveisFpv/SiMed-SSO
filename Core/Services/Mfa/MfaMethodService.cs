using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Core.Services.Mfa;

public sealed class MfaMethodService : IMfaMethodService
{
    private const string TokenLoginProvider = "SiMedSSO";
    private const string AuthenticatorTokenName = "mfa:authenticator";
    private const string EmailTokenName = "mfa:email";
    private const string EnabledValue = "enabled";

    private readonly UserManager<ApplicationUser> _userManager;

    public MfaMethodService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyCollection<string>> GetEnabledProvidersAsync(ApplicationUser user)
    {
        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return [];
        }

        var validProviders = await _userManager.GetValidTwoFactorProvidersAsync(user);
        var providers = new List<string>();

        if (validProviders.Contains(TokenOptions.DefaultAuthenticatorProvider, StringComparer.Ordinal) &&
            await IsAuthenticatorEnabledAsync(user))
        {
            providers.Add(TokenOptions.DefaultAuthenticatorProvider);
        }

        if (validProviders.Contains(TokenOptions.DefaultEmailProvider, StringComparer.Ordinal) &&
            await IsEmailEnabledAsync(user))
        {
            providers.Add(TokenOptions.DefaultEmailProvider);
        }

        if (providers.Count == 0 &&
            validProviders.Contains(TokenOptions.DefaultAuthenticatorProvider, StringComparer.Ordinal))
        {
            await SetAuthenticatorEnabledAsync(user, isEnabled: true);
            providers.Add(TokenOptions.DefaultAuthenticatorProvider);
        }

        return providers;
    }

    public async Task<bool> IsAuthenticatorEnabledAsync(ApplicationUser user)
    {
        var value = await _userManager.GetAuthenticationTokenAsync(user, TokenLoginProvider, AuthenticatorTokenName);
        return string.Equals(value, EnabledValue, StringComparison.Ordinal);
    }

    public async Task<bool> IsEmailEnabledAsync(ApplicationUser user)
    {
        var value = await _userManager.GetAuthenticationTokenAsync(user, TokenLoginProvider, EmailTokenName);
        return string.Equals(value, EnabledValue, StringComparison.Ordinal);
    }

    public Task SetAuthenticatorEnabledAsync(ApplicationUser user, bool isEnabled)
    {
        return SetMethodEnabledAsync(user, AuthenticatorTokenName, isEnabled);
    }

    public Task SetEmailEnabledAsync(ApplicationUser user, bool isEnabled)
    {
        return SetMethodEnabledAsync(user, EmailTokenName, isEnabled);
    }

    public async Task DisableAllAsync(ApplicationUser user)
    {
        await SetAuthenticatorEnabledAsync(user, isEnabled: false);
        await SetEmailEnabledAsync(user, isEnabled: false);
    }

    private async Task SetMethodEnabledAsync(ApplicationUser user, string tokenName, bool isEnabled)
    {
        if (isEnabled)
        {
            await _userManager.SetAuthenticationTokenAsync(user, TokenLoginProvider, tokenName, EnabledValue);
            return;
        }

        await _userManager.RemoveAuthenticationTokenAsync(user, TokenLoginProvider, tokenName);
    }
}
