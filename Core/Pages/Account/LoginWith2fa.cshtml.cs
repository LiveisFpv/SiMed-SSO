using System.Net;
using Core.Models;
using Core.Models.Account;
using Core.Services.Email;
using Core.Services.Mfa;
using Core.Services.Sessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[AllowAnonymous]
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;
    private readonly IMfaMethodService _mfaMethodService;
    private readonly IApplicationEmailSender _emailSender;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService,
        IMfaMethodService mfaMethodService,
        IApplicationEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userSessionService = userSessionService;
        _mfaMethodService = mfaMethodService;
        _emailSender = emailSender;
    }

    [BindProperty]
    public LoginWith2faViewModel Input { get; set; } = new();

    public IReadOnlyCollection<string> AvailableProviders { get; private set; } = [];
    public string? SelectedProvider { get; private set; }
    public string? MaskedEmail { get; private set; }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null, string? provider = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !await CanCompleteTwoFactorAsync(user))
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

        Input.RememberMe = rememberMe;
        Input.ReturnUrl = returnUrl;

        var providers = await _mfaMethodService.GetEnabledProvidersAsync(user);
        if (providers.Count == 0)
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrWhiteSpace(provider) && providers.Count == 1)
        {
            provider = providers.Single();
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            await LoadProviderStateAsync(user, providers, selectedProvider: null);
            return Page();
        }

        if (!providers.Contains(provider, StringComparer.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Выбранный MFA-метод недоступен.");
            await LoadProviderStateAsync(user, providers, selectedProvider: null);
            return Page();
        }

        Input.Provider = provider;
        await LoadProviderStateAsync(user, providers, provider);

        if (string.Equals(provider, TokenOptions.DefaultEmailProvider, StringComparison.Ordinal))
        {
            await SendEmailCodeAsync(user);
            TempData["StatusMessage"] = "Код MFA отправлен на подтвержденный email.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !await CanCompleteTwoFactorAsync(user))
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            await LoadProviderStateAsync(
                user,
                await _mfaMethodService.GetEnabledProvidersAsync(user),
                Input.Provider);
            return Page();
        }

        var providers = await _mfaMethodService.GetEnabledProvidersAsync(user);
        if (string.IsNullOrWhiteSpace(Input.Provider) ||
            !providers.Contains(Input.Provider, StringComparer.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Выберите доступный MFA-метод.");
            await LoadProviderStateAsync(user, providers, selectedProvider: null);
            return Page();
        }

        var code = NormalizeCode(Input.TwoFactorCode);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            Input.Provider,
            code);

        if (!isValid)
        {
            await RecordFailedTwoFactorAttemptAsync(user);
            ModelState.AddModelError(string.Empty, "Неверный код MFA.");
            await LoadProviderStateAsync(user, providers, Input.Provider);
            return Page();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (Input.RememberMachine)
        {
            await _signInManager.RememberTwoFactorClientAsync(user);
        }

        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        await _userSessionService.CreateSessionAndSignInAsync(HttpContext, user, Input.RememberMe);
        return RedirectToLocal(Input.ReturnUrl);
    }

    public async Task<IActionResult> OnPostSendEmailCodeAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !await CanCompleteTwoFactorAsync(user))
        {
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            return RedirectToPage("/Account/Login");
        }

        var providers = await _mfaMethodService.GetEnabledProvidersAsync(user);
        if (!providers.Contains(TokenOptions.DefaultEmailProvider, StringComparer.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "MFA через email недоступна для этого аккаунта.");
            await LoadProviderStateAsync(user, providers, selectedProvider: null);
            return Page();
        }

        Input.Provider = TokenOptions.DefaultEmailProvider;
        await SendEmailCodeAsync(user);
        await LoadProviderStateAsync(user, providers, Input.Provider);
        TempData["StatusMessage"] = "Новый код MFA отправлен на подтвержденный email.";
        return Page();
    }

    private async Task RecordFailedTwoFactorAttemptAsync(ApplicationUser user)
    {
        if (!await _userManager.GetLockoutEnabledAsync(user))
        {
            return;
        }

        await _userManager.AccessFailedAsync(user);
    }

    private async Task<bool> CanCompleteTwoFactorAsync(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            return false;
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            return false;
        }

        return !await _userManager.IsLockedOutAsync(user);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    private static string NormalizeCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

    private async Task LoadProviderStateAsync(
        ApplicationUser user,
        IReadOnlyCollection<string> providers,
        string? selectedProvider)
    {
        AvailableProviders = providers;
        SelectedProvider = selectedProvider;
        MaskedEmail = MaskEmail(await _userManager.GetEmailAsync(user));
    }

    private async Task SendEmailCodeAsync(ApplicationUser user)
    {
        var email = await _userManager.GetEmailAsync(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email пользователя не найден.");
        }

        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        var body = $"""
            <p>Код MFA для входа в SiMed SSO:</p>
            <p><strong style="font-size: 20px;">{WebUtility.HtmlEncode(code)}</strong></p>
            <p>Если вы не выполняли вход, просто проигнорируйте это письмо.</p>
            """;

        await _emailSender.SendEmailAsync(email, "Код MFA для входа в SiMed SSO", body);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 1)
        {
            return $"***{email[atIndex..]}";
        }

        return $"{email[0]}***{email[atIndex..]}";
    }
}
