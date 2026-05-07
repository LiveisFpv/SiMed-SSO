using Core.Identity;
using Core.Models.Admin;
using Core.Services.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin.Clients;

[Authorize(Roles = ApplicationRoles.Admin)]
public class DetailsModel : PageModel
{
    private readonly IOAuthClientService _clients;

    public DetailsModel(IOAuthClientService clients)
    {
        _clients = clients;
    }

    public OAuthClientDetailsViewModel Client { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var client = await _clients.GetClientDetailsAsync(id, HttpContext.RequestAborted);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        if (!await _clients.SetActiveAsync(id, isActive: false, HttpContext.RequestAborted))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "OAuth client was deactivated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid id)
    {
        if (!await _clients.SetActiveAsync(id, isActive: true, HttpContext.RequestAborted))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "OAuth client was reactivated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRotateSecretAsync(Guid id)
    {
        var result = await _clients.RotateSecretAsync(id, HttpContext.RequestAborted);
        if (result is null)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Client secret was rotated. Copy the new secret now; it will not be shown again.";
        TempData["CreatedClientId"] = result.ClientId;
        TempData["CreatedClientSecret"] = result.ClientSecret;
        return RedirectToPage(new { id });
    }
}
