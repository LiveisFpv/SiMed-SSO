using Core.Identity;
using Core.Models.Admin;
using Core.Services.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin.Clients;

[Authorize(Roles = ApplicationRoles.Admin)]
public class IndexModel : PageModel
{
    private readonly IOAuthClientService _clients;

    public IndexModel(IOAuthClientService clients)
    {
        _clients = clients;
    }

    public IReadOnlyCollection<OAuthClientListItemViewModel> Clients { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Clients = await _clients.GetClientsAsync(HttpContext.RequestAborted);
    }
}
