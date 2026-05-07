using System.ComponentModel.DataAnnotations;

namespace Core.Models.Admin;

public sealed class OAuthClientListItemViewModel
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool RequirePkce { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public IReadOnlyCollection<string> RedirectUris { get; set; } = [];
    public IReadOnlyCollection<string> Scopes { get; set; } = [];
}

public sealed class OAuthClientDetailsViewModel
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool RequirePkce { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public IReadOnlyCollection<string> RedirectUris { get; set; } = [];
    public IReadOnlyCollection<string> Scopes { get; set; } = [];
}

public sealed class OAuthClientCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool RequirePkce { get; set; } = true;

    [Display(Name = "Redirect URIs")]
    public string RedirectUrisText { get; set; } = string.Empty;

    public List<OAuthScopeSelectionViewModel> Scopes { get; set; } = [];
}

public sealed class OAuthClientEditViewModel
{
    public Guid Id { get; set; }
    public string? ClientId { get; set; }

    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool RequirePkce { get; set; } = true;

    [Display(Name = "Redirect URIs")]
    public string RedirectUrisText { get; set; } = string.Empty;

    public List<OAuthScopeSelectionViewModel> Scopes { get; set; } = [];
}

public sealed class OAuthScopeSelectionViewModel
{
    public required string Scope { get; set; }
    public bool IsSelected { get; set; }
}

public sealed record CreatedOAuthClientResult(Guid Id, string ClientId, string ClientSecret);
