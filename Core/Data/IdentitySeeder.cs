using Core.Identity;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Core.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{role}': {FormatErrors(result)}");
                }
            }
        }

        var adminEmail = configuration["SSO_ADMIN_EMAIL"];
        var adminPassword = configuration["SSO_ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            await EnsureOAuthClientEndpointPermissionsAsync(applicationManager);
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create admin user '{adminEmail}': {FormatErrors(createResult)}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, ApplicationRoles.Admin);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add admin role to '{adminEmail}': {FormatErrors(addRoleResult)}");
            }
        }

        await EnsureOAuthClientEndpointPermissionsAsync(applicationManager);
    }

    private static async Task EnsureOAuthClientEndpointPermissionsAsync(IOpenIddictApplicationManager applicationManager)
    {
        var applications = new List<object>();
        await foreach (var application in applicationManager.ListAsync(count: null, offset: null))
        {
            applications.Add(application);
        }

        foreach (var application in applications)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(descriptor, application);

            var isActive = descriptor.Permissions.Contains(Permissions.Endpoints.Authorization) &&
                descriptor.Permissions.Contains(Permissions.Endpoints.Token);
            if (!isActive)
            {
                continue;
            }

            var changed = false;
            changed |= descriptor.Permissions.Add(Permissions.Endpoints.Revocation);
            changed |= descriptor.Permissions.Add(Permissions.Endpoints.Introspection);

            if (changed)
            {
                await applicationManager.UpdateAsync(application, descriptor);
            }
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
