using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Core.Options;

public static class RateLimitingOptions
{
    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await context.HttpContext.Response.WriteAsync(
                "Слишком много запросов. Повторите попытку позже.",
                cancellationToken);
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var category = GetCategory(context.Request.Path);
            if (category is null)
            {
                return RateLimitPartition.GetNoLimiter("default");
            }

            var partitionKey = $"{category}:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => CreateLimiter(category));
        });
    }

    private static string? GetCategory(PathString path)
    {
        if (path.StartsWithSegments("/connect/token") ||
            path.StartsWithSegments("/connect/revocation") ||
            path.StartsWithSegments("/connect/introspection"))
        {
            return "oidc-token";
        }

        if (path.StartsWithSegments("/connect/authorize"))
        {
            return "oidc-authorize";
        }

        if (path.StartsWithSegments("/Account/Login") ||
            path.StartsWithSegments("/Account/Register") ||
            path.StartsWithSegments("/Account/ForgotPassword") ||
            path.StartsWithSegments("/Account/ResetPassword") ||
            path.StartsWithSegments("/Account/ResendEmailConfirmation") ||
            path.StartsWithSegments("/Account/LoginWith2fa") ||
            path.StartsWithSegments("/Account/LoginWithRecoveryCode") ||
            path.StartsWithSegments("/Account/Manage/Mfa"))
        {
            return "auth-strict";
        }

        return null;
    }

    private static FixedWindowRateLimiterOptions CreateLimiter(string category)
    {
        var permitLimit = category switch
        {
            "oidc-token" => 30,
            "oidc-authorize" => 60,
            _ => 10
        };

        return new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        };
    }
}
