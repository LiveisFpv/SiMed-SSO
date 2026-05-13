using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SampleClient.Models;
using SampleClient.Services;

namespace SampleClient.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly IUserInfoClient _userInfoClient;

    public ProfileModel(IUserInfoClient userInfoClient)
    {
        _userInfoClient = userInfoClient;
    }

    public IReadOnlyCollection<ClaimViewModel> Claims { get; private set; } = [];
    public IReadOnlyCollection<TokenViewModel> Tokens { get; private set; } = [];
    public UserInfoResultViewModel? UserInfo { get; private set; }

    public async Task OnGetAsync()
    {
        Claims = User.Claims
            .OrderBy(claim => claim.Type, StringComparer.Ordinal)
            .Select(claim => new ClaimViewModel(claim.Type, claim.Value))
            .ToArray();

        var accessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");
        var idToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "id_token");
        var refreshToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "refresh_token");
        var expiresAt = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "expires_at");

        Tokens =
        [
            CreateTokenView("access_token", accessToken, expiresAt),
            CreateTokenView("id_token", idToken, null),
            CreateTokenView("refresh_token", refreshToken, null)
        ];

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            UserInfo = await _userInfoClient.GetUserInfoAsync(accessToken, HttpContext.RequestAborted);
        }
    }

    private static TokenViewModel CreateTokenView(string name, string? value, string? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new TokenViewModel(name, IsPresent: false, Length: 0, Preview: "-", ExpiresAt: expiresAt);
        }

        return new TokenViewModel(
            name,
            IsPresent: true,
            value.Length,
            Mask(value),
            expiresAt);
    }

    private static string Mask(string value)
    {
        if (value.Length <= 20)
        {
            return "***";
        }

        return $"{value[..12]}...{value[^6..]}";
    }
}
