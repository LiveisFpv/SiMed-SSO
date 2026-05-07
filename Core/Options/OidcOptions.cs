using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Core.Options;

public sealed class OidcOptions
{
    public string? Issuer { get; init; }
    public string? SigningCertificatePath { get; init; }
    public string? SigningCertificatePassword { get; init; }
    public string? EncryptionCertificatePath { get; init; }
    public string? EncryptionCertificatePassword { get; init; }

    public static OidcOptions FromConfiguration(IConfiguration configuration)
    {
        return new OidcOptions
        {
            Issuer = configuration["SSO_ISSUER"],
            SigningCertificatePath = configuration["OIDC_SIGNING_CERT_PATH"],
            SigningCertificatePassword = configuration["OIDC_SIGNING_CERT_PASSWORD"],
            EncryptionCertificatePath = configuration["OIDC_ENCRYPTION_CERT_PATH"],
            EncryptionCertificatePassword = configuration["OIDC_ENCRYPTION_CERT_PASSWORD"]
        };
    }

    public X509Certificate2 LoadSigningCertificate(IHostEnvironment environment) =>
        LoadCertificate(
            SigningCertificatePath,
            SigningCertificatePassword,
            "OIDC_SIGNING_CERT_PATH",
            environment);

    public X509Certificate2 LoadEncryptionCertificate(IHostEnvironment environment) =>
        LoadCertificate(
            EncryptionCertificatePath,
            EncryptionCertificatePassword,
            "OIDC_ENCRYPTION_CERT_PATH",
            environment);

    private static X509Certificate2 LoadCertificate(
        string? configuredPath,
        string? password,
        string settingName,
        IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                $"{settingName} is not configured. Set both OIDC signing and encryption .pfx certificate paths in non-Development environments.");
        }

        var path = ResolvePath(configuredPath, environment.ContentRootPath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"{settingName} points to a missing certificate file: '{path}'.");
        }

        try
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                path,
                password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable,
                loaderLimits: null);

            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    $"{settingName} certificate must contain a private key.");
            }

            return certificate;
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException(
                $"Unable to load the certificate configured by {settingName}. Check that the .pfx file is valid and the password is correct.",
                exception);
        }
    }

    private static string ResolvePath(string path, string contentRootPath)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }
}
