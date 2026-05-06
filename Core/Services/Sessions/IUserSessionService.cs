using Core.Models;

namespace Core.Services.Sessions;

public interface IUserSessionService
{
    Task CreateSessionAndSignInAsync(HttpContext httpContext, ApplicationUser user, bool isPersistent);
    Task RefreshSignInWithCurrentSessionAsync(HttpContext httpContext, ApplicationUser user);
    Task RevokeCurrentSessionAsync(HttpContext httpContext, string reason);
    Task RevokeAllUserSessionsAsync(ApplicationUser user, string reason, string? revokedByUserId);
    Task UpdateLastSeenAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserSessionViewModel>> GetUserSessionsAsync(
        string userId,
        Guid? currentSessionId,
        CancellationToken cancellationToken = default);
    Task DeleteOldSessionsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default);
}
