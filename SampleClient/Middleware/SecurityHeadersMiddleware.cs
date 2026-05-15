namespace SampleClient.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicyReportOnly =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        headers.TryAdd("Content-Security-Policy-Report-Only", ContentSecurityPolicyReportOnly);

        await _next(context);
    }
}
