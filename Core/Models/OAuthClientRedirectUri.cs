namespace Core.Models;

public sealed class OAuthClientRedirectUri
{
    public required string ClientId { get; set; }
    public required string Uri { get; set; }

    public OAuthClient Client { get; set; } = null!;
}
