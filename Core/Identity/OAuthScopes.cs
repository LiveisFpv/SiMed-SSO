namespace Core.Identity;

public static class OAuthScopes
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";

    public static readonly IReadOnlyCollection<string> All =
    [
        OpenId,
        Profile,
        Email,
        OfflineAccess
    ];
}
