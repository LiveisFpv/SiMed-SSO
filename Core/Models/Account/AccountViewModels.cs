using System.ComponentModel.DataAnnotations;

namespace Core.Models.Account;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль.")]
    [StringLength(100, MinimumLength = 9, ErrorMessage = "Пароль должен быть от {2} до {1} символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
    [Display(Name = "Подтверждение пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите новый пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
    [Display(Name = "Подтверждение пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Отсутствует код сброса пароля.")]
    public string Code { get; set; } = string.Empty;
}

public sealed class ResendEmailConfirmationViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
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
    [Required(ErrorMessage = "Укажите текущий пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите новый пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают.")]
    [Display(Name = "Подтверждение пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangeEmailViewModel
{
    [Display(Name = "Текущий email")]
    public string? CurrentEmail { get; set; }

    [Required(ErrorMessage = "Укажите новый email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Новый email")]
    public string NewEmail { get; set; } = string.Empty;
}

public sealed class LoginWith2faViewModel
{
    [Required(ErrorMessage = "Укажите код из приложения.")]
    [Display(Name = "Код из приложения")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "Запомнить этот браузер")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class LoginWithRecoveryCodeViewModel
{
    [Required(ErrorMessage = "Укажите recovery code.")]
    [Display(Name = "Recovery code")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class EnableAuthenticatorViewModel
{
    [Required(ErrorMessage = "Укажите код из приложения.")]
    [Display(Name = "Код из приложения")]
    public string VerificationCode { get; set; } = string.Empty;
}

public sealed class ConfirmPasswordViewModel
{
    [Required(ErrorMessage = "Укажите текущий пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;
}
