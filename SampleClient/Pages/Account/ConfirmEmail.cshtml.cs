using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SampleClient.Models;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class ConfirmEmailModel : PageModel
{
    private readonly UserManager<SampleApplicationUser> _userManager;

    public ConfirmEmailModel(UserManager<SampleApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public string StatusMessage { get; private set; } = "Email не подтвержден.";

    public async Task OnGetAsync(string? userId = null, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            StatusMessage = "Ссылка подтверждения некорректна.";
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            StatusMessage = "Ссылка подтверждения некорректна.";
            return;
        }

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
        StatusMessage = result.Succeeded
            ? "Email подтвержден. Теперь можно войти."
            : "Не удалось подтвердить email.";
    }
}
