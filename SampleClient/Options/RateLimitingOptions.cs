using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SampleClient.Options;

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
            if (!IsAuthPath(context.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter("default");
            }

            var partitionKey = $"auth:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
    }

    private static bool IsAuthPath(PathString path)
    {
        return path.StartsWithSegments("/Account/Login") ||
               path.StartsWithSegments("/Account/Register") ||
               path.StartsWithSegments("/Account/ExternalLogin") ||
               path.StartsWithSegments("/Account/ForgotPassword") ||
               path.StartsWithSegments("/Account/ResetPassword") ||
               path.StartsWithSegments("/Account/ResendEmailConfirmation");
    }
}
