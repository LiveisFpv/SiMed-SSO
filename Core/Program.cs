using Core.Data;
using Core.Identity;
using Core.Middleware;
using Core.Models;
using Core.Options;
using Core.Services.Email;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Utils;

DbUtils.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

builder.Services.AddSingleton(smtpOptions);
builder.Services.AddTransient<IApplicationEmailSender>(services =>
    smtpOptions.IsConfigured
        ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(services)
        : ActivatorUtilities.CreateInstance<DevelopmentEmailSender>(services));
builder.Services.AddTransient<IdentityEmailSender>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>>(services =>
    services.GetRequiredService<IdentityEmailSender>());
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddHostedService<UserSessionCleanupService>();

builder.Services.AddDbContext<ApplicationDbContext>(options=>
    options.UseNpgsql(connectionString));

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
    .AddSignInManager<ApplicationSignInManager>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<UserSessionTrackingMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();
