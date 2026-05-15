using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SampleClient.Authentication;
using SampleClient.Data;
using SampleClient.Middleware;
using SampleClient.Models;
using SampleClient.Options;
using SampleClient.Services;
using SampleClient.Utils;

DotEnvLoader.LoadSampleClientEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var connectionString = SampleClientDatabaseOptions.GetConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException(
        "SampleClient database settings were not found. Set 'ConnectionStrings__SampleClient' or SAMPLECLIENT_POSTGRES_* environment variables.");

var oidcOptions = SampleClientOidcOptions.FromConfiguration(builder.Configuration);
if (string.IsNullOrWhiteSpace(oidcOptions.ClientId))
{
    throw new InvalidOperationException("SampleClient client id is not configured. Set SAMPLECLIENT_CLIENT_ID.");
}

if (string.IsNullOrWhiteSpace(oidcOptions.ClientSecret))
{
    throw new InvalidOperationException("SampleClient client secret is not configured. Set SAMPLECLIENT_CLIENT_SECRET.");
}

var identityOptions = SampleClientIdentityOptions.FromConfiguration(builder.Configuration, builder.Environment);
var smtpOptions = SampleClientSmtpOptions.FromConfiguration(builder.Configuration);

ProductionReadinessValidator.Validate(
    builder.Configuration,
    builder.Environment,
    connectionString,
    oidcOptions,
    smtpOptions);

DataProtectionKeyOptions.AddConfiguredDataProtection(
    builder.Services,
    builder.Configuration,
    builder.Environment);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    ForwardedHeadersConfiguration.Configure(options, builder.Configuration));
builder.Services.AddRateLimiter(SampleClient.Options.RateLimitingOptions.Configure);

builder.Services.AddSingleton(oidcOptions);
builder.Services.AddSingleton(identityOptions);
builder.Services.AddSingleton(smtpOptions);
builder.Services.AddTransient<ISampleClientEmailSender>(services =>
    smtpOptions.IsConfigured
        ? ActivatorUtilities.CreateInstance<SmtpSampleClientEmailSender>(services)
        : ActivatorUtilities.CreateInstance<LoggingSampleClientEmailSender>(services));
builder.Services.AddDbContext<SampleClientDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddHttpClient<IUserInfoClient, UserInfoClient>(client =>
{
    client.BaseAddress = new Uri(oidcOptions.Authority);
});

builder.Services.AddIdentity<SampleApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = identityOptions.RequireEmailVerification;
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 9;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<SampleClientDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SiMed.SampleClient";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Error";
});

builder.Services
    .AddAuthentication()
    .AddOpenIdConnect(SampleClientAuthenticationSchemes.SiMedSso, "SiMed SSO", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClaimsIssuer = "SiMedSSO";
        options.Authority = oidcOptions.Authority;
        options.ClientId = oidcOptions.ClientId;
        options.ClientSecret = oidcOptions.ClientSecret;
        options.CallbackPath = oidcOptions.CallbackPath;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        foreach (var scope in oidcOptions.Scopes)
        {
            options.Scope.Add(scope);
        }
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
    .AllowAnonymous();

app.MapGet("/health/ready", GetReadinessAsync)
    .AllowAnonymous();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task<IResult> GetReadinessAsync(
    SampleClientDbContext dbContext,
    SampleClientSmtpOptions smtpOptions,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken)
{
    var checks = new Dictionary<string, object?>();
    var healthy = true;

    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        checks["database"] = canConnect ? "Healthy" : "Unhealthy";
        healthy &= canConnect;

        if (canConnect)
        {
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            checks["migrations"] = pendingMigrations.Length == 0
                ? "Healthy"
                : $"Unhealthy: pending migrations: {string.Join(", ", pendingMigrations)}";
            healthy &= pendingMigrations.Length == 0;
        }
    }
    catch (Exception exception)
    {
        checks["database"] = $"Unhealthy: {exception.GetType().Name}";
        healthy = false;
    }

    if (environment.IsDevelopment())
    {
        checks["smtp"] = smtpOptions.IsConfigured ? "Configured" : "Development sender";
    }
    else
    {
        checks["smtp"] = smtpOptions.IsConfigured ? "Healthy" : "Unhealthy";
        healthy &= smtpOptions.IsConfigured;
    }

    return Results.Json(
        new { status = healthy ? "Healthy" : "Unhealthy", checks },
        statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}
