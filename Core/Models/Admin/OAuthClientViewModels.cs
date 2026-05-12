using System.ComponentModel.DataAnnotations;

namespace Core.Models.Admin;

public sealed class OAuthClientListItemViewModel
{
    public required string Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool RequirePkce { get; set; }
    public IReadOnlyCollection<string> RedirectUris { get; set; } = [];
    public IReadOnlyCollection<string> Scopes { get; set; } = [];
}

public sealed class OAuthClientDetailsViewModel
{
    public required string Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool RequirePkce { get; set; }
    public string? CreatedByUserId { get; set; }
    public IReadOnlyCollection<string> RedirectUris { get; set; } = [];
    public IReadOnlyCollection<string> Scopes { get; set; } = [];
}

public sealed class OAuthClientCreateViewModel
{
    [Required(ErrorMessage = "Укажите название.")]
    [StringLength(200, ErrorMessage = "Название должно быть не длиннее {1} символов.")]
    [Display(Name = "Название")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Описание должно быть не длиннее {1} символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Display(Name = "Требовать PKCE")]
    public bool RequirePkce { get; set; } = true;

    [Display(Name = "Redirect URIs")]
    public string RedirectUrisText { get; set; } = string.Empty;

    public List<OAuthScopeSelectionViewModel> Scopes { get; set; } = [];
}

public sealed class OAuthClientEditViewModel
{
    public string? Id { get; set; }
    public string? ClientId { get; set; }

    [Required(ErrorMessage = "Укажите название.")]
    [StringLength(200, ErrorMessage = "Название должно быть не длиннее {1} символов.")]
    [Display(Name = "Название")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Описание должно быть не длиннее {1} символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Display(Name = "Требовать PKCE")]
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

public sealed record CreatedOAuthClientResult(string Id, string ClientId, string ClientSecret);
