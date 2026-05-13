using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SampleClient.Options;
using SampleClient.Services;
using SampleClient.Utils;

DotEnvLoader.LoadSampleClientEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var oidcOptions = SampleClientOidcOptions.FromConfiguration(builder.Configuration);
if (string.IsNullOrWhiteSpace(oidcOptions.ClientId))
{
    throw new InvalidOperationException("SampleClient client id is not configured. Set SAMPLECLIENT_CLIENT_ID.");
}

if (string.IsNullOrWhiteSpace(oidcOptions.ClientSecret))
{
    throw new InvalidOperationException("SampleClient client secret is not configured. Set SAMPLECLIENT_CLIENT_SECRET.");
}

builder.Services.AddSingleton(oidcOptions);
builder.Services.AddHttpClient<IUserInfoClient, UserInfoClient>(client =>
{
    client.BaseAddress = new Uri(oidcOptions.Authority);
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "SiMed.SampleClient";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/";
        options.AccessDeniedPath = "/Error";
    })
    .AddOpenIdConnect(options =>
    {
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
