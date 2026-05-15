using Npgsql;

namespace SampleClient.Options;

public static class ProductionReadinessValidator
{
    private static readonly string[] PlaceholderFragments =
    [
        "changeme",
        "change-this",
        "replace-with",
        "simed_replace",
        "userpass",
        "your@mail.com",
        "user@mail.com",
        "example.com"
    ];

    public static void Validate(
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionString,
        SampleClientOidcOptions oidcOptions,
        SampleClientSmtpOptions smtpOptions)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        if (!Uri.TryCreate(oidcOptions.Authority, UriKind.Absolute, out var authority) ||
            authority.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "SAMPLECLIENT_AUTHORITY must be an absolute HTTPS URI outside Development.");
        }

        if (!smtpOptions.IsConfigured)
        {
            throw new InvalidOperationException(
                "SampleClient SMTP settings are required outside Development. Set SAMPLECLIENT_SMTP_HOST, SAMPLECLIENT_SMTP_PORT and SAMPLECLIENT_FROM_EMAIL.");
        }

        RejectPlaceholder("SAMPLECLIENT_POSTGRES_PASSWORD", GetPassword(connectionString));
        RejectPlaceholder("SAMPLECLIENT_CLIENT_ID", oidcOptions.ClientId);
        RejectPlaceholder("SAMPLECLIENT_CLIENT_SECRET", oidcOptions.ClientSecret);
        RejectPlaceholder("SAMPLECLIENT_SMTP_PASSWORD", smtpOptions.Password);
        RejectPlaceholder("SAMPLECLIENT_FROM_EMAIL", smtpOptions.FromEmail);
    }

    private static string? GetPassword(string connectionString)
    {
        try
        {
            return new NpgsqlConnectionStringBuilder(connectionString).Password;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void RejectPlaceholder(string settingName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();
        if (string.Equals(normalized, "0000", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "postgres", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "ChangeMe!123", StringComparison.OrdinalIgnoreCase) ||
            PlaceholderFragments.Any(fragment =>
                normalized.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"{settingName} contains a development placeholder and must be changed outside Development.");
        }
    }
}
