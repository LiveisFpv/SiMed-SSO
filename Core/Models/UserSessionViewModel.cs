namespace Core.Models;

public sealed class UserSessionViewModel
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Device { get; set; }
    public bool IsCurrent { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
