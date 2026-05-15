using System.ComponentModel.DataAnnotations;

namespace SampleClient.Models;

public sealed class LocalLoginViewModel
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

public sealed class LocalRegisterViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Имя должно быть не длиннее {1} символов.")]
    [Display(Name = "Имя")]
    public string? DisplayName { get; set; }

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

public sealed class ResendConfirmationViewModel
{
    [Required(ErrorMessage = "Укажите email.")]
    [EmailAddress(ErrorMessage = "Укажите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
