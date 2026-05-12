using System.Text;
using Core.Models;
using Core.Models.Account;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace Core.Pages.Account.Manage;

[Authorize]
public class MfaModel : PageModel
{
    private const string AuthenticatorIssuer = "SiMed SSO";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserSessionService _userSessionService;

    public MfaModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserSessionService userSessionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userSessionService = userSessionService;
    }

    [BindProperty]
    public EnableAuthenticatorViewModel EnableInput { get; set; } = new();

    [BindProperty]
    public ConfirmPasswordViewModel PasswordInput { get; set; } = new();

    public bool IsMfaEnabled { get; private set; }
    public int RecoveryCodesLeft { get; private set; }
    public string SharedKey { get; private set; } = string.Empty;
    public string AuthenticatorUri { get; private set; } = string.Empty;
    public string QrCodeSvg { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> RecoveryCodes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        await LoadAsync(user);
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var user = await GetCurrentUserAsync();
        await EnsureAuthenticatorKeyAsync(user);

        ModelState.Clear();
        if (!TryValidateModel(EnableInput, nameof(EnableInput)))
        {
            await LoadAsync(user);
            return Page();
        }

        var code = NormalizeCode(EnableInput.VerificationCode);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);

        if (!isValid)
        {
            ModelState.AddModelError("EnableInput.VerificationCode", "Неверный код из приложения-аутентификатора.");
            await LoadAsync(user);
            return Page();
        }

        var enableResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enableResult.Succeeded)
        {
            AddIdentityErrors(enableResult);
            await LoadAsync(user);
            return Page();
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = recoveryCodes?.ToArray() ?? [];
        await _userSessionService.RefreshSignInWithCurrentSessionAsync(HttpContext, user);
        TempData["StatusMessage"] = "MFA включена. Сохраните recovery codes сейчас.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateRecoveryCodesAsync()
    {
        var user = await GetCurrentUserAsync();
        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return RedirectToPage();
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = recoveryCodes?.ToArray() ?? [];
        TempData["StatusMessage"] = "Новые recovery codes сгенерированы. Сохраните их сейчас.";
        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var user = await GetCurrentUserAsync();
        if (!await ValidateCurrentPasswordAsync(user))
        {
            await LoadAsync(user);
            return Page();
        }

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            await LoadAsync(user);
            return Page();
        }

        await _signInManager.ForgetTwoFactorClientAsync();
        await _userSessionService.RefreshSignInWithCurrentSessionAsync(HttpContext, user);
        TempData["StatusMessage"] = "MFA отключена.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetAuthenticatorAsync()
    {
        var user = await GetCurrentUserAsync();
        if (!await ValidateCurrentPasswordAsync(user))
        {
            await LoadAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _signInManager.ForgetTwoFactorClientAsync();
        await _userSessionService.RefreshSignInWithCurrentSessionAsync(HttpContext, user);
        TempData["StatusMessage"] = "Authenticator key сброшен. Настройте MFA заново, чтобы включить ее.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        await EnsureAuthenticatorKeyAsync(user);

        IsMfaEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

        var email = await _userManager.GetEmailAsync(user) ?? await _userManager.GetUserNameAsync(user) ?? user.Id;
        var key = await _userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;

        SharedKey = FormatKey(key);
        AuthenticatorUri = BuildAuthenticatorUri(email, key);
        QrCodeSvg = GenerateQrCodeSvg(AuthenticatorUri);
    }

    private async Task EnsureAuthenticatorKeyAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(await _userManager.GetAuthenticatorKeyAsync(user)))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
        }
    }

    private async Task<bool> ValidateCurrentPasswordAsync(ApplicationUser user)
    {
        ModelState.Clear();
        if (!TryValidateModel(PasswordInput, nameof(PasswordInput)))
        {
            return false;
        }

        if (await _userManager.CheckPasswordAsync(user, PasswordInput.CurrentPassword))
        {
            return true;
        }

        ModelState.AddModelError("PasswordInput.CurrentPassword", "Неверный пароль.");
        return false;
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user ?? throw new InvalidOperationException("Текущий пользователь не найден.");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string NormalizeCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

    private static string FormatKey(string key)
    {
        var result = new StringBuilder();
        var normalized = key.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        for (var index = 0; index < normalized.Length; index++)
        {
            if (index > 0 && index % 4 == 0)
            {
                result.Append(' ');
            }

            result.Append(normalized[index]);
        }

        return result.ToString();
    }

    private static string BuildAuthenticatorUri(string email, string key)
    {
        return string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            Uri.EscapeDataString(AuthenticatorIssuer),
            Uri.EscapeDataString(email),
            Uri.EscapeDataString(key));
    }

    private static string GenerateQrCodeSvg(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(data);
        return qrCode.GetGraphic(4);
    }
}
