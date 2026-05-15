using Npgsql;

namespace SampleClient.Options;

public static class SampleClientDatabaseOptions
{
    public static string? GetConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("SampleClient") ??
               BuildPostgresConnectionString(configuration);
    }

    private static string? BuildPostgresConnectionString(IConfiguration configuration)
    {
        var host = Get(configuration, "SAMPLECLIENT_POSTGRES_HOST", "SAMPLE_POSTGRES_HOST");
        var database = Get(
            configuration,
            "SAMPLECLIENT_POSTGRES_DATABASE",
            "SAMPLECLIENT_POSTGRES_DB",
            "SAMPLE_POSTGRES_DATABASE",
            "SAMPLE_POSTGRES_DB");
        var username = Get(configuration, "SAMPLECLIENT_POSTGRES_USERNAME", "SAMPLE_POSTGRES_USER");
        var password = Get(configuration, "SAMPLECLIENT_POSTGRES_PASSWORD", "SAMPLE_POSTGRES_PASSWORD");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.TryParse(Get(configuration, "SAMPLECLIENT_POSTGRES_PORT", "SAMPLE_POSTGRES_PORT"), out var port)
                ? port
                : 5432,
            Database = database,
            Username = username,
            Password = password
        };

        return builder.ConnectionString;
    }

    private static string? Get(IConfiguration configuration, params string[] keys)
    {
        return keys
            .Select(key => configuration[key])
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
