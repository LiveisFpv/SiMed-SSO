using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SampleClient.Authentication;
using SampleClient.Models;
using SampleClient.Services;

namespace SampleClient.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<SampleApplicationUser> _userManager;
    private readonly IUserInfoClient _userInfoClient;

    public ProfileModel(
        UserManager<SampleApplicationUser> userManager,
        IUserInfoClient userInfoClient)
    {
        _userManager = userManager;
        _userInfoClient = userInfoClient;
    }

    public LocalUserProfileViewModel? LocalUser { get; private set; }
    public IReadOnlyCollection<ClaimViewModel> Claims { get; private set; } = [];
    public IReadOnlyCollection<TokenViewModel> Tokens { get; private set; } = [];
    public UserInfoResultViewModel? UserInfo { get; private set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("Текущий пользователь не найден.");

        var logins = await _userManager.GetLoginsAsync(user);
        var hasSiMedSsoLogin = logins.Any(login =>
            string.Equals(login.LoginProvider, SampleClientAuthenticationSchemes.SiMedSso, StringComparison.Ordinal));
        var hasPassword = await _userManager.HasPasswordAsync(user);

        LocalUser = new LocalUserProfileViewModel(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.EmailConfirmed,
            user.SsoSubject,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            hasPassword,
            hasSiMedSsoLogin);

        Claims = User.Claims
            .OrderBy(claim => claim.Type, StringComparer.Ordinal)
            .Select(claim => new ClaimViewModel(claim.Type, claim.Value))
            .ToArray();

        var accessToken = await _userManager.GetAuthenticationTokenAsync(
            user,
            SampleClientAuthenticationSchemes.SiMedSso,
            "access_token");
        var idToken = await _userManager.GetAuthenticationTokenAsync(
            user,
            SampleClientAuthenticationSchemes.SiMedSso,
            "id_token");
        var refreshToken = await _userManager.GetAuthenticationTokenAsync(
            user,
            SampleClientAuthenticationSchemes.SiMedSso,
            "refresh_token");
        var expiresAt = await _userManager.GetAuthenticationTokenAsync(
            user,
            SampleClientAuthenticationSchemes.SiMedSso,
            "expires_at");

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
