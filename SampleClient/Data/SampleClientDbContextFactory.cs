using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SampleClient.Options;
using SampleClient.Utils;

namespace SampleClient.Data;

public sealed class SampleClientDbContextFactory : IDesignTimeDbContextFactory<SampleClientDbContext>
{
    public SampleClientDbContext CreateDbContext(string[] args)
    {
        DotEnvLoader.LoadSampleClientEnv();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = SampleClientDatabaseOptions.GetConnectionString(configuration)
            ?? "Host=localhost;Port=5432;Database=simed_sso_sample_client;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<SampleClientDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SampleClientDbContext(options);
    }
}
