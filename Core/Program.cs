using Core.Data;
using Core.Identity;
using Core.Middleware;
using Core.Models;
using Core.Options;
using Core.Services.Email;
using Core.Services.Mfa;
using Core.Services.OAuth;
using Core.Services.Sessions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Utils;
using OpenIddict.Abstractions;

DbUtils.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? DbUtils.BuildPostgresConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException(
        "Database connection settings were not found. Set 'ConnectionStrings__DefaultConnection' or POSTGRES_* environment variables.");

var smtpOptions = SmtpOptions.FromConfiguration(builder.Configuration);
if (!builder.Environment.IsDevelopment() && !smtpOptions.IsConfigured)
{
    throw new InvalidOperationException(
        "SMTP settings were not found. Set SMTP_HOST, SMTP_PORT, FROM_EMAIL and optional SMTP_USERNAME/SMTP_PASSWORD.");
}

var oidcOptions = OidcOptions.FromConfiguration(builder.Configuration);
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(oidcOptions.Issuer))
{
    throw new InvalidOperationException("OIDC issuer was not configured. Set SSO_ISSUER in non-Development environments.");
}

ProductionReadinessValidator.Validate(
    builder.Configuration,
    builder.Environment,
    connectionString,
    smtpOptions,
    oidcOptions);

DataProtectionKeyOptions.AddConfiguredDataProtection(
    builder.Services,
    builder.Configuration,
    builder.Environment);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    ForwardedHeadersConfiguration.Configure(options, builder.Configuration));
builder.Services.AddRateLimiter(Core.Options.RateLimitingOptions.Configure);

builder.Services.AddSingleton(smtpOptions);
builder.Services.AddSingleton(oidcOptions);
builder.Services.AddTransient<IApplicationEmailSender>(services =>
    smtpOptions.IsConfigured
        ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(services)
        : ActivatorUtilities.CreateInstance<DevelopmentEmailSender>(services));
builder.Services.AddTransient<IdentityEmailSender>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>>(services =>
    services.GetRequiredService<IdentityEmailSender>());
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddHostedService<UserSessionCleanupService>();
builder.Services.AddScoped<IMfaMethodService, MfaMethodService>();
builder.Services.AddScoped<IOAuthClientService, OAuthClientService>();
builder.Services.AddScoped<OAuthClaimsPrincipalFactory>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = smtpOptions.RequireEmailVerification;
        
        options.Password.RequiredLength = 9;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        
        options.User.RequireUniqueEmail=true;
        // options.Tokens.PasswordResetTokenProvider=
        // options.ClaimsIdentity.RoleClaimType=
    })
    .AddErrorDescriber<RussianIdentityErrorDescriber>()
    .AddSignInManager<ApplicationSignInManager>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetRevocationEndpointUris("/connect/revocation")
            .SetIntrospectionEndpointUris("/connect/introspection")
            .SetConfigurationEndpointUris("/.well-known/openid-configuration")
            .SetJsonWebKeySetEndpointUris("/.well-known/jwks")
            .SetUserInfoEndpointUris("/connect/userinfo");

        options.AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange()
            .AllowRefreshTokenFlow();

        options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5));
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess);

        if (!string.IsNullOrWhiteSpace(oidcOptions.Issuer))
        {
            options.SetIssuer(new Uri(oidcOptions.Issuer));
        }

        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            options.AddEncryptionCertificate(oidcOptions.LoadEncryptionCertificate(builder.Environment))
                .AddSigningCertificate(oidcOptions.LoadSigningCertificate(builder.Environment));
        }

        var aspNetCore = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration();

        if (builder.Environment.IsDevelopment())
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/simed-sso.yaml", "SiMed SSO OIDC API");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SiMed SSO API";
    });

    app.MapGet("/openapi/simed-sso.yaml", (IWebHostEnvironment environment) =>
    {
        var path = Path.Combine(environment.ContentRootPath, "..", "docs", "openapi", "simed-sso.yaml");
        if (!File.Exists(path))
        {
            return Results.NotFound();
        }

        return Results.File(path, "application/yaml");
    });
}

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<UserSessionTrackingMiddleware>();
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
    ApplicationDbContext dbContext,
    SmtpOptions smtpOptions,
    OidcOptions oidcOptions,
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
        checks["oidcCertificates"] = "Development certificates";
    }
    else
    {
        checks["smtp"] = smtpOptions.IsConfigured ? "Healthy" : "Unhealthy";
        healthy &= smtpOptions.IsConfigured;

        try
        {
            using var signingCertificate = oidcOptions.LoadSigningCertificate(environment);
            using var encryptionCertificate = oidcOptions.LoadEncryptionCertificate(environment);
            checks["oidcCertificates"] = "Healthy";
        }
        catch (Exception exception)
        {
            checks["oidcCertificates"] = $"Unhealthy: {exception.GetType().Name}";
            healthy = false;
        }
    }

    return Results.Json(
        new { status = healthy ? "Healthy" : "Unhealthy", checks },
        statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}
