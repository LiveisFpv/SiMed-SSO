using System.Security.Cryptography;
using Core.Identity;
using Core.Models.Admin;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Core.Services.OAuth;

public sealed class OAuthClientService : IOAuthClientService
{
    private const int ClientIdBytes = 24;
    private const int ClientSecretBytes = 32;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IWebHostEnvironment _environment;

    public OAuthClientService(
        IOpenIddictApplicationManager applicationManager,
        IWebHostEnvironment environment)
    {
        _applicationManager = applicationManager;
        _environment = environment;
    }

    public async Task<IReadOnlyCollection<OAuthClientListItemViewModel>> GetClientsAsync(
        CancellationToken cancellationToken = default)
    {
        var applications = new List<object>();
        await foreach (var application in _applicationManager.ListAsync(count: null, offset: null, cancellationToken))
        {
            applications.Add(application);
        }

        var clients = new List<OAuthClientListItemViewModel>();

        foreach (var application in applications)
        {
            var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
            clients.Add(new OAuthClientListItemViewModel
            {
                Id = await RequireIdAsync(application, cancellationToken),
                ClientId = await RequireClientIdAsync(application, cancellationToken),
                DisplayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken) ?? "OAuth client без имени",
                IsActive = IsActive(permissions),
                RequirePkce = await RequiresPkceAsync(application, cancellationToken),
                RedirectUris = await GetRedirectUrisAsync(application, cancellationToken),
                Scopes = GetAllowedScopes(await _applicationManager.GetSettingsAsync(application, cancellationToken), permissions)
            });
        }

        return clients
            .OrderBy(client => client.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<OAuthClientDetailsViewModel?> GetClientDetailsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
        var settings = await _applicationManager.GetSettingsAsync(application, cancellationToken);

        return new OAuthClientDetailsViewModel
        {
            Id = await RequireIdAsync(application, cancellationToken),
            ClientId = await RequireClientIdAsync(application, cancellationToken),
            DisplayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken) ?? "OAuth client без имени",
            Description = settings.GetValueOrDefault(OAuthClientSettings.Description),
            IsActive = IsActive(permissions),
            RequirePkce = await RequiresPkceAsync(application, cancellationToken),
            CreatedByUserId = settings.GetValueOrDefault(OAuthClientSettings.CreatedByUserId),
            RedirectUris = await GetRedirectUrisAsync(application, cancellationToken),
            Scopes = GetAllowedScopes(settings, permissions)
        };
    }

    public async Task<OAuthClientEditViewModel?> GetEditModelAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var permissions = await _applicationManager.GetPermissionsAsync(application, cancellationToken);
        var settings = await _applicationManager.GetSettingsAsync(application, cancellationToken);

        return new OAuthClientEditViewModel
        {
            Id = await RequireIdAsync(application, cancellationToken),
            ClientId = await RequireClientIdAsync(application, cancellationToken),
            DisplayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken) ?? string.Empty,
            Description = settings.GetValueOrDefault(OAuthClientSettings.Description),
            RequirePkce = await RequiresPkceAsync(application, cancellationToken),
            RedirectUrisText = string.Join(Environment.NewLine, await GetRedirectUrisAsync(application, cancellationToken)),
            Scopes = CreateScopeSelections(GetAllowedScopes(settings, permissions))
        };
    }

    public OAuthClientCreateViewModel CreateEmptyCreateModel()
    {
        return new OAuthClientCreateViewModel
        {
            RequirePkce = true,
            Scopes = CreateScopeSelections(OAuthScopes.All)
        };
    }

    public async Task<CreatedOAuthClientResult> CreateClientAsync(
        OAuthClientCreateViewModel model,
        string? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var clientId = await GenerateUniqueClientIdAsync(cancellationToken);
        var clientSecret = GenerateSecret();
        var descriptor = CreateDescriptor(
            clientId,
            clientSecret,
            model.DisplayName,
            model.Description,
            model.RequirePkce,
            ParseRedirectUris(model.RedirectUrisText),
            GetSelectedScopes(model.Scopes),
            createdByUserId);

        var application = await _applicationManager.CreateAsync(descriptor, cancellationToken);
        return new CreatedOAuthClientResult(
            await RequireIdAsync(application, cancellationToken),
            clientId,
            clientSecret);
    }

    public async Task<bool> UpdateClientAsync(
        OAuthClientEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            return false;
        }

        var application = await _applicationManager.FindByIdAsync(model.Id, cancellationToken);
        if (application is null)
        {
            return false;
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        var isActive = IsActive(descriptor.Permissions);
        descriptor.DisplayName = model.DisplayName.Trim();
        descriptor.RedirectUris.Clear();
        descriptor.Requirements.Clear();
        descriptor.Permissions.Clear();
        var selectedScopes = GetSelectedScopes(model.Scopes).ToArray();
        descriptor.Settings[OAuthClientSettings.Description] = string.IsNullOrWhiteSpace(model.Description)
            ? string.Empty
            : model.Description.Trim();
        descriptor.Settings[OAuthClientSettings.Scopes] = string.Join(" ", selectedScopes);

        foreach (var redirectUri in ParseRedirectUris(model.RedirectUrisText))
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        AddDefaultPermissions(descriptor.Permissions, selectedScopes, isActive);
        if (model.RequirePkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveAsync(
        string id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return false;
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        if (isActive)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.Revocation);
            descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
        }
        else
        {
            descriptor.Permissions.Remove(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Remove(Permissions.Endpoints.Token);
            descriptor.Permissions.Remove(Permissions.Endpoints.Revocation);
            descriptor.Permissions.Remove(Permissions.Endpoints.Introspection);
        }

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
        return true;
    }

    public async Task<CreatedOAuthClientResult?> RotateSecretAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByIdAsync(id, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var clientSecret = GenerateSecret();
        await _applicationManager.UpdateAsync(application, clientSecret, cancellationToken);

        return new CreatedOAuthClientResult(
            await RequireIdAsync(application, cancellationToken),
            await RequireClientIdAsync(application, cancellationToken),
            clientSecret);
    }

    public void ValidateClientInput(OAuthClientCreateViewModel model, ModelStateDictionaryAdapter modelState)
    {
        ValidateRedirectUris(model.RedirectUrisText, "Input.RedirectUrisText", modelState);
        ValidateScopes(model.Scopes, "Input.Scopes", modelState);
    }

    public void ValidateClientInput(OAuthClientEditViewModel model, ModelStateDictionaryAdapter modelState)
    {
        ValidateRedirectUris(model.RedirectUrisText, "Input.RedirectUrisText", modelState);
        ValidateScopes(model.Scopes, "Input.Scopes", modelState);
    }

    private OpenIddictApplicationDescriptor CreateDescriptor(
        string clientId,
        string clientSecret,
        string displayName,
        string? description,
        bool requirePkce,
        IEnumerable<string> redirectUris,
        IEnumerable<string> scopes,
        string? createdByUserId)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Web,
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Explicit,
            DisplayName = displayName.Trim()
        };

        var selectedScopes = scopes.ToArray();
        descriptor.Settings[OAuthClientSettings.Description] = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        descriptor.Settings[OAuthClientSettings.CreatedByUserId] = createdByUserId ?? string.Empty;
        descriptor.Settings[OAuthClientSettings.Scopes] = string.Join(" ", selectedScopes);

        foreach (var redirectUri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        AddDefaultPermissions(descriptor.Permissions, selectedScopes, isActive: true);
        if (requirePkce)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        return descriptor;
    }

    private async Task<string> GenerateUniqueClientIdAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var clientId = $"simed_{GenerateToken(ClientIdBytes)}";
            if (await _applicationManager.FindByClientIdAsync(clientId, cancellationToken) is null)
            {
                return clientId;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique OAuth client id.");
    }

    private static void AddDefaultPermissions(
        ISet<string> permissions,
        IEnumerable<string> scopes,
        bool isActive)
    {
        if (isActive)
        {
            permissions.Add(Permissions.Endpoints.Authorization);
            permissions.Add(Permissions.Endpoints.Token);
            permissions.Add(Permissions.Endpoints.Revocation);
            permissions.Add(Permissions.Endpoints.Introspection);
        }

        permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        permissions.Add(Permissions.GrantTypes.RefreshToken);
        permissions.Add(Permissions.ResponseTypes.Code);

        foreach (var scope in scopes)
        {
            switch (scope)
            {
                case OAuthScopes.Email:
                    permissions.Add(Permissions.Scopes.Email);
                    break;
                case OAuthScopes.Profile:
                    permissions.Add(Permissions.Scopes.Profile);
                    break;
            }
        }
    }

    private static bool IsActive(IEnumerable<string> permissions)
    {
        var set = permissions.ToHashSet(StringComparer.Ordinal);
        return set.Contains(Permissions.Endpoints.Authorization) &&
               set.Contains(Permissions.Endpoints.Token);
    }

    private async Task<bool> RequiresPkceAsync(object application, CancellationToken cancellationToken)
    {
        var requirements = await _applicationManager.GetRequirementsAsync(application, cancellationToken);
        return requirements.Contains(Requirements.Features.ProofKeyForCodeExchange, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyCollection<string>> GetRedirectUrisAsync(
        object application,
        CancellationToken cancellationToken)
    {
        var redirectUris = await _applicationManager.GetRedirectUrisAsync(application, cancellationToken);
        return redirectUris
            .OrderBy(uri => uri, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyCollection<string> GetAllowedScopes(
        IReadOnlyDictionary<string, string> settings,
        IEnumerable<string> permissions)
    {
        if (settings.TryGetValue(OAuthClientSettings.Scopes, out var configuredScopes) &&
            !string.IsNullOrWhiteSpace(configuredScopes))
        {
            return configuredScopes
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(scope => scope, StringComparer.Ordinal)
                .ToArray();
        }

        var set = permissions.ToHashSet(StringComparer.Ordinal);
        var scopes = new List<string> { OAuthScopes.OpenId };
        if (set.Contains(Permissions.Scopes.Profile))
        {
            scopes.Add(OAuthScopes.Profile);
        }

        if (set.Contains(Permissions.Scopes.Email))
        {
            scopes.Add(OAuthScopes.Email);
        }

        return scopes
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<string> RequireIdAsync(object application, CancellationToken cancellationToken)
    {
        return await _applicationManager.GetIdAsync(application, cancellationToken)
               ?? throw new InvalidOperationException("OpenIddict application id was not found.");
    }

    private async Task<string> RequireClientIdAsync(object application, CancellationToken cancellationToken)
    {
        return await _applicationManager.GetClientIdAsync(application, cancellationToken)
               ?? throw new InvalidOperationException("OpenIddict client id was not found.");
    }

    private static string GenerateSecret() => GenerateToken(ClientSecretBytes);

    private static string GenerateToken(int bytes)
    {
        var data = RandomNumberGenerator.GetBytes(bytes);
        return WebEncoders.Base64UrlEncode(data);
    }

    private void ValidateRedirectUris(
        string redirectUrisText,
        string key,
        ModelStateDictionaryAdapter modelState)
    {
        var redirectUris = ParseRedirectUris(redirectUrisText).ToArray();
        if (redirectUris.Length == 0)
        {
            modelState.AddError(key, "Укажите хотя бы один redirect URI.");
            return;
        }

        foreach (var redirectUri in redirectUris)
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                modelState.AddError(key, $"Redirect URI '{redirectUri}' должен быть абсолютным HTTP или HTTPS URI.");
                continue;
            }

            if (uri.Scheme == Uri.UriSchemeHttp &&
                (!_environment.IsDevelopment() ||
                 !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
            {
                modelState.AddError(key, $"Redirect URI '{redirectUri}' должен использовать HTTPS вне localhost development.");
            }
        }
    }

    private static void ValidateScopes(
        IReadOnlyCollection<OAuthScopeSelectionViewModel> scopes,
        string key,
        ModelStateDictionaryAdapter modelState)
    {
        var selectedScopes = GetSelectedScopes(scopes).ToArray();
        if (selectedScopes.Length == 0)
        {
            modelState.AddError(key, "Выберите хотя бы один scope.");
            return;
        }

        var allowedScopes = OAuthScopes.All.ToHashSet(StringComparer.Ordinal);
        foreach (var scope in selectedScopes)
        {
            if (!allowedScopes.Contains(scope))
            {
                modelState.AddError(key, $"Scope '{scope}' не разрешен.");
            }
        }
    }

    private static IEnumerable<string> ParseRedirectUris(string redirectUrisText)
    {
        return redirectUrisText
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(uri => uri, StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetSelectedScopes(IEnumerable<OAuthScopeSelectionViewModel> scopes)
    {
        return scopes
            .Where(scope => scope.IsSelected)
            .Select(scope => scope.Scope)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal);
    }

    private static List<OAuthScopeSelectionViewModel> CreateScopeSelections(IEnumerable<string> selectedScopes)
    {
        var selected = selectedScopes.ToHashSet(StringComparer.Ordinal);
        return OAuthScopes.All
            .Select(scope => new OAuthScopeSelectionViewModel
            {
                Scope = scope,
                IsSelected = selected.Contains(scope)
            })
            .ToList();
    }
}
