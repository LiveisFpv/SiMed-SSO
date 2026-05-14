using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SampleClient.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnPostLogin()
    {
        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Profile")
            },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostLogout()
    {
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Index")
            },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
