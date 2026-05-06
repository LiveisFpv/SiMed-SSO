using Core.Services.Sessions;

namespace Core.Middleware;

public sealed class UserSessionTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public UserSessionTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserSessionService userSessionService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await userSessionService.UpdateLastSeenAsync(context, context.RequestAborted);
        }

        await _next(context);
    }
}
