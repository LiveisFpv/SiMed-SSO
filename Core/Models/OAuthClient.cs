namespace Core.Models;

public sealed class OAuthClient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ClientId { get; set; }
    public required string ClientSecretHash { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequirePkce { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public ICollection<OAuthClientRedirectUri> RedirectUris { get; set; } = [];
    public ICollection<OAuthClientScope> Scopes { get; set; } = [];
}
