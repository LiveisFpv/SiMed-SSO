namespace Core.Options;

public sealed class SmtpOptions
{
    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FromEmail { get; init; }
    public string FromName { get; init; } = "SiMed SSO";
    public bool RequireEmailVerification { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        Port > 0 &&
        !string.IsNullOrWhiteSpace(FromEmail);

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);

    public static SmtpOptions FromConfiguration(IConfiguration configuration)
    {
        return new SmtpOptions
        {
            Host = configuration["SMTP_HOST"],
            Port = ParsePort(configuration["SMTP_PORT"]),
            Username = configuration["SMTP_USERNAME"],
            Password = configuration["SMTP_PASSWORD"],
            FromEmail = configuration["FROM_EMAIL"],
            FromName = GetValueOrDefault(configuration["FROM_NAME"], "SiMed SSO"),
            RequireEmailVerification = bool.TryParse(configuration["SSO_REQUIRE_EMAIL_VERIFICATION"], out var requireEmailVerification) &&
                requireEmailVerification
        };
    }

    private static int ParsePort(string? value) =>
        int.TryParse(value, out var port) ? port : 587;

    private static string GetValueOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}
