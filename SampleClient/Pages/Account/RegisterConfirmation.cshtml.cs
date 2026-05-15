using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class RegisterConfirmationModel : PageModel
{
    public string Email { get; private set; } = string.Empty;

    public void OnGet(string? email = null)
    {
        Email = email ?? "указанного email";
    }
}
