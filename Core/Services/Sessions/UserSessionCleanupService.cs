namespace Core.Services.Sessions;

public sealed class UserSessionCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(90);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserSessionCleanupService> _logger;

    public UserSessionCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserSessionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionService>();
            await sessions.DeleteOldSessionsAsync(DateTimeOffset.UtcNow.Subtract(Retention), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up old user sessions.");
        }
    }
}
