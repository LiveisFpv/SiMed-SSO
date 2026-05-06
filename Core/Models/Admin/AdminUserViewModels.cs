namespace Core.Models.Admin;

public sealed class AdminUserListItemViewModel
{
    public required string Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}

public sealed class AdminUserDetailsViewModel
{
    public required string Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<UserSessionViewModel> Sessions { get; set; } = [];
}

public sealed class EditUserRolesViewModel
{
    public required string UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public List<RoleSelectionViewModel> Roles { get; set; } = [];
}

public sealed class RoleSelectionViewModel
{
    public required string RoleName { get; set; }
    public bool IsSelected { get; set; }
}
