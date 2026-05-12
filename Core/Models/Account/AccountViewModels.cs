using System.ComponentModel.DataAnnotations;

namespace Core.Models.Account;

public sealed class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 9)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

public sealed class ResendEmailConfirmationViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ManageAccountViewModel
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangeEmailViewModel
{
    public string? CurrentEmail { get; set; }

    [Required]
    [EmailAddress]
    public string NewEmail { get; set; } = string.Empty;
}

public sealed class LoginWith2faViewModel
{
    [Required]
    [Display(Name = "Authenticator code")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "Remember this browser")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class LoginWithRecoveryCodeViewModel
{
    [Required]
    [Display(Name = "Recovery code")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class EnableAuthenticatorViewModel
{
    [Required]
    [Display(Name = "Authenticator code")]
    public string VerificationCode { get; set; } = string.Empty;
}

public sealed class ConfirmPasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;
}
