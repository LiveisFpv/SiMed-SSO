namespace Core.Models;

public sealed class OAuthClientScope
{
    public required string ClientId { get; set; }
    public required string Scope { get; set; }

    public OAuthClient Client { get; set; } = null!;
}
