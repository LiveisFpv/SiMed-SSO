using Core.Models;

namespace Core.Services.Mfa;

public interface IMfaMethodService
{
    Task<IReadOnlyCollection<string>> GetEnabledProvidersAsync(ApplicationUser user);
    Task<bool> IsAuthenticatorEnabledAsync(ApplicationUser user);
    Task<bool> IsEmailEnabledAsync(ApplicationUser user);
    Task SetAuthenticatorEnabledAsync(ApplicationUser user, bool isEnabled);
    Task SetEmailEnabledAsync(ApplicationUser user, bool isEnabled);
    Task DisableAllAsync(ApplicationUser user);
}
