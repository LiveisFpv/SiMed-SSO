using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    public void OnGet()
    {
    }
}
