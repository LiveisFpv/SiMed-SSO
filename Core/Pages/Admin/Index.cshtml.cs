using Core.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin;

[Authorize(Roles = ApplicationRoles.Admin)]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
