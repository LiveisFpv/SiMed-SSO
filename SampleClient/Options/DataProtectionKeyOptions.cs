using Microsoft.AspNetCore.DataProtection;

namespace SampleClient.Options;

public static class DataProtectionKeyOptions
{
    public static void AddConfiguredDataProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var builder = services
            .AddDataProtection()
            .SetApplicationName("SiMed-SampleClient");

        if (environment.IsDevelopment())
        {
            return;
        }

        var keysPath = configuration["SAMPLECLIENT_DATA_PROTECTION_KEYS_PATH"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            throw new InvalidOperationException(
                "SAMPLECLIENT_DATA_PROTECTION_KEYS_PATH is not configured. Persistent Data Protection keys are required outside Development.");
        }

        var directory = EnsureWritableDirectory(keysPath, environment.ContentRootPath, "SAMPLECLIENT_DATA_PROTECTION_KEYS_PATH");
        builder.PersistKeysToFileSystem(directory);
    }

    private static DirectoryInfo EnsureWritableDirectory(string configuredPath, string contentRootPath, string settingName)
    {
        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));

        try
        {
            Directory.CreateDirectory(path);
            var probePath = Path.Combine(path, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probePath, "ok");
            File.Delete(probePath);
            return new DirectoryInfo(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"{settingName} points to a directory that cannot be created or written: '{path}'.",
                exception);
        }
    }
}
