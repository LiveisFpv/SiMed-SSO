namespace SampleClient.Options;

public sealed class SampleClientOidcOptions
{
    public string Authority { get; init; } = "https://localhost:7269/";
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string CallbackPath { get; init; } = "/signin-oidc";
    public IReadOnlyCollection<string> Scopes { get; init; } =
    [
        "openid",
        "profile",
        "email",
        "offline_access"
    ];

    public Uri UserInfoEndpoint => new(new Uri(Authority), "/connect/userinfo");

    public static SampleClientOidcOptions FromConfiguration(IConfiguration configuration)
    {
        var authority = configuration["SAMPLECLIENT_AUTHORITY"] ?? "https://localhost:7269/";
        if (!authority.EndsWith('/'))
        {
            authority += "/";
        }

        return new SampleClientOidcOptions
        {
            Authority = authority,
            ClientId = configuration["SAMPLECLIENT_CLIENT_ID"],
            ClientSecret = configuration["SAMPLECLIENT_CLIENT_SECRET"],
            CallbackPath = configuration["SAMPLECLIENT_CALLBACK_PATH"] ?? "/signin-oidc"
        };
    }
}
