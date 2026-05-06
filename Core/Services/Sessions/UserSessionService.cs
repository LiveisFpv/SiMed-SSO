using System.Security.Claims;
using Core.Data;
using Core.Identity;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Sessions;

public sealed class UserSessionService : IUserSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan LastSeenThrottle = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _dbContext;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(
        ApplicationDbContext dbContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<UserSessionService> logger)
    {
        _dbContext = dbContext;
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task CreateSessionAndSignInAsync(HttpContext httpContext, ApplicationUser user, bool isPersistent)
    {
        var session = CreateSession(httpContext, user.Id);

        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync(httpContext.RequestAborted);

        await SignInWithSessionAsync(user, isPersistent, session.Id);
    }

    public async Task RefreshSignInWithCurrentSessionAsync(HttpContext httpContext, ApplicationUser user)
    {
        var sessionId = GetCurrentSessionId(httpContext);
        if (sessionId is null)
        {
            await CreateSessionAndSignInAsync(httpContext, user, isPersistent: false);
            return;
        }

        await SignInWithSessionAsync(user, isPersistent: false, sessionId.Value);
    }

    public async Task RevokeCurrentSessionAsync(HttpContext httpContext, string reason)
    {
        var sessionId = GetCurrentSessionId(httpContext);
        var userId = _userManager.GetUserId(httpContext.User);
        if (sessionId is null || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(item => item.Id == sessionId.Value && item.UserId == userId, httpContext.RequestAborted);

        if (session is null || session.RevokedAtUtc is not null)
        {
            return;
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        session.RevokedByUserId = userId;
        session.RevokeReason = reason;

        await _dbContext.SaveChangesAsync(httpContext.RequestAborted);
    }

    public async Task RevokeAllUserSessionsAsync(ApplicationUser user, string reason, string? revokedByUserId)
    {
        var now = DateTimeOffset.UtcNow;
        var activeSessions = await _dbContext.UserSessions
            .Where(session => session.UserId == user.Id && session.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = now;
            session.RevokedByUserId = revokedByUserId;
            session.RevokeReason = reason;
        }

        await _dbContext.SaveChangesAsync();

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            _logger.LogWarning("Failed to update security stamp for user {UserId}: {Errors}",
                user.Id,
                string.Join("; ", stampResult.Errors.Select(error => error.Description)));
        }
    }

    public async Task UpdateLastSeenAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var sessionId = GetCurrentSessionId(httpContext);
        var userId = _userManager.GetUserId(httpContext.User);
        if (sessionId is null || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                item => item.Id == sessionId.Value &&
                        item.UserId == userId &&
                        item.RevokedAtUtc == null,
                cancellationToken);

        if (session is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - session.LastSeenAtUtc < LastSeenThrottle)
        {
            return;
        }

        session.LastSeenAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserSessionViewModel>> GetUserSessionsAsync(
        string userId,
        Guid? currentSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserSessions
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.LastSeenAtUtc)
            .Select(session => new UserSessionViewModel
            {
                Id = session.Id,
                CreatedAtUtc = session.CreatedAtUtc,
                LastSeenAtUtc = session.LastSeenAtUtc,
                ExpiresAtUtc = session.ExpiresAtUtc,
                RevokedAtUtc = session.RevokedAtUtc,
                IpAddress = session.IpAddress,
                Browser = session.Browser,
                OperatingSystem = session.OperatingSystem,
                Device = session.Device,
                IsCurrent = currentSessionId.HasValue && session.Id == currentSessionId.Value
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task DeleteOldSessionsAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserSessions
            .Where(session =>
                (session.RevokedAtUtc != null && session.RevokedAtUtc < cutoffUtc) ||
                session.ExpiresAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private UserSession CreateSession(HttpContext httpContext, string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var parsed = UserAgentParser.Parse(userAgent);

        return new UserSession
        {
            UserId = userId,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.Add(SessionLifetime),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent,
            Browser = parsed.Browser,
            OperatingSystem = parsed.OperatingSystem,
            Device = parsed.Device
        };
    }

    private Task SignInWithSessionAsync(ApplicationUser user, bool isPersistent, Guid sessionId)
    {
        var claims = new[]
        {
            new Claim(ApplicationClaimTypes.SessionId, sessionId.ToString("D"))
        };

        return _signInManager.SignInWithClaimsAsync(user, isPersistent, claims);
    }

    public static Guid? GetCurrentSessionId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ApplicationClaimTypes.SessionId);
        return Guid.TryParse(value, out var sessionId) ? sessionId : null;
    }
}
