using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SampleClient.Authentication;
using SampleClient.Data;
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

builder.Services.AddSingleton(oidcOptions);
builder.Services.AddSingleton(identityOptions);
builder.Services.AddTransient<ISampleClientEmailSender, LoggingSampleClientEmailSender>();
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

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
