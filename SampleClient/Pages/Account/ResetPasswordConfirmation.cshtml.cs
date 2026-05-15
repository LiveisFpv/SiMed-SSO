using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SampleClient.Pages.Account;

[AllowAnonymous]
public sealed class ResetPasswordConfirmationModel : PageModel
{
    public void OnGet()
    {
    }
}
