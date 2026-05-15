namespace SampleClient.Options;

public sealed class SampleClientSmtpOptions
{
    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FromEmail { get; init; }
    public string FromName { get; init; } = "SiMed SampleClient";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        Port > 0 &&
        !string.IsNullOrWhiteSpace(FromEmail);

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);

    public static SampleClientSmtpOptions FromConfiguration(IConfiguration configuration)
    {
        return new SampleClientSmtpOptions
        {
            Host = configuration["SAMPLECLIENT_SMTP_HOST"],
            Port = int.TryParse(configuration["SAMPLECLIENT_SMTP_PORT"], out var port) ? port : 587,
            Username = configuration["SAMPLECLIENT_SMTP_USERNAME"],
            Password = configuration["SAMPLECLIENT_SMTP_PASSWORD"],
            FromEmail = configuration["SAMPLECLIENT_FROM_EMAIL"],
            FromName = string.IsNullOrWhiteSpace(configuration["SAMPLECLIENT_FROM_NAME"])
                ? "SiMed SampleClient"
                : configuration["SAMPLECLIENT_FROM_NAME"]!
        };
    }
}
