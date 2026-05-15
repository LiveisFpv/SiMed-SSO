namespace SampleClient.Options;

public sealed class SampleClientIdentityOptions
{
    public bool RequireEmailVerification { get; init; }

    public static SampleClientIdentityOptions FromConfiguration(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var configured = configuration["SAMPLECLIENT_REQUIRE_EMAIL_VERIFICATION"];
        var requireEmailVerification = bool.TryParse(configured, out var value)
            ? value
            : !environment.IsDevelopment();

        return new SampleClientIdentityOptions
        {
            RequireEmailVerification = requireEmailVerification
        };
    }
}
