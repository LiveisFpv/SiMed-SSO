using Npgsql;

namespace Core.Options;

public static class ProductionReadinessValidator
{
    private static readonly string[] PlaceholderFragments =
    [
        "changeme",
        "change-this",
        "replace-with",
        "userpass",
        "pgadminpass",
        "your@mail.com",
        "user@mail.com",
        "example.com"
    ];

    public static void Validate(
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionString,
        SmtpOptions smtpOptions,
        OidcOptions oidcOptions)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        RequireHttpsIssuer(oidcOptions.Issuer);

        if (!smtpOptions.IsConfigured)
        {
            throw new InvalidOperationException(
                "SMTP settings are required outside Development. Set SMTP_HOST, SMTP_PORT and FROM_EMAIL.");
        }

        RejectPlaceholder("POSTGRES_PASSWORD", GetPassword(connectionString));
        RejectPlaceholder("SSO_ADMIN_PASSWORD", configuration["SSO_ADMIN_PASSWORD"]);
        RejectPlaceholder("SMTP_PASSWORD", smtpOptions.Password);
        RejectPlaceholder("FROM_EMAIL", smtpOptions.FromEmail);
        RejectPlaceholder("OIDC_SIGNING_CERT_PASSWORD", oidcOptions.SigningCertificatePassword);
        RejectPlaceholder("OIDC_ENCRYPTION_CERT_PASSWORD", oidcOptions.EncryptionCertificatePassword);

        using var signingCertificate = oidcOptions.LoadSigningCertificate(environment);
        using var encryptionCertificate = oidcOptions.LoadEncryptionCertificate(environment);
    }

    private static void RequireHttpsIssuer(string? issuer)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "SSO_ISSUER must be an absolute HTTPS URI outside Development.");
        }
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
