using Microsoft.AspNetCore.Identity;

namespace SampleClient.Models;

public sealed class SampleApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? SsoSubject { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAtUtc { get; set; }
}
