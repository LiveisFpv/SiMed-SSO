using System.Security.Cryptography;
using Core.Data;
using Core.Identity;
using Core.Models;
using Core.Models.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.OAuth;

public sealed class OAuthClientService : IOAuthClientService
{
    private const int ClientIdBytes = 24;
    private const int ClientSecretBytes = 32;

    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<OAuthClient> _passwordHasher;
    private readonly IWebHostEnvironment _environment;

    public OAuthClientService(
        ApplicationDbContext dbContext,
        IPasswordHasher<OAuthClient> passwordHasher,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    public async Task<IReadOnlyCollection<OAuthClientListItemViewModel>> GetClientsAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = await _dbContext.Set<OAuthClient>()
            .AsNoTracking()
            .Include(client => client.RedirectUris)
            .Include(client => client.Scopes)
            .OrderBy(client => client.DisplayName)
            .ToArrayAsync(cancellationToken);

        return clients
            .Select(client => new OAuthClientListItemViewModel
            {
                Id = client.Id,
                ClientId = client.ClientId,
                DisplayName = client.DisplayName,
                IsActive = client.IsActive,
                RequirePkce = client.RequirePkce,
                CreatedAtUtc = client.CreatedAtUtc,
                UpdatedAtUtc = client.UpdatedAtUtc,
                RedirectUris = client.RedirectUris
                    .OrderBy(uri => uri.Uri)
                    .Select(uri => uri.Uri)
                    .ToArray(),
                Scopes = client.Scopes
                    .OrderBy(scope => scope.Scope)
                    .Select(scope => scope.Scope)
                    .ToArray()
            })
            .ToArray();
    }

    public async Task<OAuthClientDetailsViewModel?> GetClientDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Set<OAuthClient>()
            .AsNoTracking()
            .Include(item => item.RedirectUris)
            .Include(item => item.Scopes)
            .Where(item => item.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            return null;
        }

        return new OAuthClientDetailsViewModel
        {
            Id = client.Id,
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            Description = client.Description,
            IsActive = client.IsActive,
            RequirePkce = client.RequirePkce,
            CreatedAtUtc = client.CreatedAtUtc,
            UpdatedAtUtc = client.UpdatedAtUtc,
            CreatedByUserId = client.CreatedByUserId,
            RedirectUris = client.RedirectUris
                .OrderBy(uri => uri.Uri)
                .Select(uri => uri.Uri)
                .ToArray(),
            Scopes = client.Scopes
                .OrderBy(scope => scope.Scope)
                .Select(scope => scope.Scope)
                .ToArray()
        };
    }

    public async Task<OAuthClientEditViewModel?> GetEditModelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Set<OAuthClient>()
            .AsNoTracking()
            .Include(item => item.RedirectUris)
            .Include(item => item.Scopes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (client is null)
        {
            return null;
        }

        var selectedScopes = client.Scopes.Select(scope => scope.Scope).ToHashSet(StringComparer.Ordinal);
        return new OAuthClientEditViewModel
        {
            Id = client.Id,
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            Description = client.Description,
            RequirePkce = client.RequirePkce,
            RedirectUrisText = string.Join(Environment.NewLine, client.RedirectUris
                .OrderBy(uri => uri.Uri)
                .Select(uri => uri.Uri)),
            Scopes = CreateScopeSelections(selectedScopes)
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
        var now = DateTimeOffset.UtcNow;
        var clientId = await GenerateUniqueClientIdAsync(cancellationToken);
        var clientSecret = GenerateSecret();

        var client = new OAuthClient
        {
            ClientId = clientId,
            ClientSecretHash = string.Empty,
            DisplayName = model.DisplayName.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            IsActive = true,
            RequirePkce = model.RequirePkce,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = createdByUserId
        };
        client.ClientSecretHash = _passwordHasher.HashPassword(client, clientSecret);

        foreach (var redirectUri in ParseRedirectUris(model.RedirectUrisText))
        {
            client.RedirectUris.Add(new OAuthClientRedirectUri
            {
                ClientId = client.ClientId,
                Uri = redirectUri
            });
        }

        foreach (var scope in GetSelectedScopes(model.Scopes))
        {
            client.Scopes.Add(new OAuthClientScope
            {
                ClientId = client.ClientId,
                Scope = scope
            });
        }

        _dbContext.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedOAuthClientResult(client.Id, client.ClientId, clientSecret);
    }

    public async Task<bool> UpdateClientAsync(
        OAuthClientEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Set<OAuthClient>()
            .Include(item => item.RedirectUris)
            .Include(item => item.Scopes)
            .FirstOrDefaultAsync(item => item.Id == model.Id, cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.DisplayName = model.DisplayName.Trim();
        client.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        client.RequirePkce = model.RequirePkce;
        client.UpdatedAtUtc = DateTimeOffset.UtcNow;

        UpdateRedirectUris(client, ParseRedirectUris(model.RedirectUrisText));
        UpdateScopes(client, GetSelectedScopes(model.Scopes));

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Set<OAuthClient>()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.IsActive = isActive;
        client.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CreatedOAuthClientResult?> RotateSecretAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Set<OAuthClient>()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (client is null)
        {
            return null;
        }

        var clientSecret = GenerateSecret();
        client.ClientSecretHash = _passwordHasher.HashPassword(client, clientSecret);
        client.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new CreatedOAuthClientResult(client.Id, client.ClientId, clientSecret);
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

    private async Task<string> GenerateUniqueClientIdAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var clientId = $"simed_{GenerateToken(ClientIdBytes)}";
            var exists = await _dbContext.Set<OAuthClient>()
                .AnyAsync(client => client.ClientId == clientId, cancellationToken);

            if (!exists)
            {
                return clientId;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique OAuth client id.");
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
            modelState.AddError(key, "At least one redirect URI is required.");
            return;
        }

        foreach (var redirectUri in redirectUris)
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                modelState.AddError(key, $"Redirect URI '{redirectUri}' must be an absolute HTTP or HTTPS URI.");
                continue;
            }

            if (uri.Scheme == Uri.UriSchemeHttp &&
                (!_environment.IsDevelopment() ||
                 !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
            {
                modelState.AddError(key, $"Redirect URI '{redirectUri}' must use HTTPS outside localhost development.");
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
            modelState.AddError(key, "At least one scope is required.");
            return;
        }

        var allowedScopes = OAuthScopes.All.ToHashSet(StringComparer.Ordinal);
        foreach (var scope in selectedScopes)
        {
            if (!allowedScopes.Contains(scope))
            {
                modelState.AddError(key, $"Scope '{scope}' is not allowed.");
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

    private static void UpdateRedirectUris(OAuthClient client, IEnumerable<string> redirectUris)
    {
        var next = redirectUris.ToHashSet(StringComparer.Ordinal);
        var existing = client.RedirectUris.Select(item => item.Uri).ToHashSet(StringComparer.Ordinal);

        foreach (var redirectUri in client.RedirectUris.Where(item => !next.Contains(item.Uri)).ToArray())
        {
            client.RedirectUris.Remove(redirectUri);
        }

        foreach (var redirectUri in next.Except(existing, StringComparer.Ordinal))
        {
            client.RedirectUris.Add(new OAuthClientRedirectUri
            {
                ClientId = client.ClientId,
                Uri = redirectUri
            });
        }
    }

    private static void UpdateScopes(OAuthClient client, IEnumerable<string> scopes)
    {
        var next = scopes.ToHashSet(StringComparer.Ordinal);
        var existing = client.Scopes.Select(item => item.Scope).ToHashSet(StringComparer.Ordinal);

        foreach (var scope in client.Scopes.Where(item => !next.Contains(item.Scope)).ToArray())
        {
            client.Scopes.Remove(scope);
        }

        foreach (var scope in next.Except(existing, StringComparer.Ordinal))
        {
            client.Scopes.Add(new OAuthClientScope
            {
                ClientId = client.ClientId,
                Scope = scope
            });
        }
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
