namespace SampleClient.Models;

public sealed record LocalUserProfileViewModel(
    string Id,
    string Email,
    string? DisplayName,
    bool EmailConfirmed,
    string? SsoSubject,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    bool HasPassword,
    bool HasSiMedSsoLogin);

public sealed record ClaimViewModel(string Type, string Value);

public sealed record TokenViewModel(
    string Name,
    bool IsPresent,
    int Length,
    string Preview,
    string? ExpiresAt);

public sealed record UserInfoResultViewModel(
    bool IsSuccess,
    int StatusCode,
    string Body);
