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

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var client = await _clients.GetClientDetailsAsync(id, HttpContext.RequestAborted);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(string id)
    {
        if (!await _clients.SetActiveAsync(id, isActive: false, HttpContext.RequestAborted))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "OAuth client деактивирован.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReactivateAsync(string id)
    {
        if (!await _clients.SetActiveAsync(id, isActive: true, HttpContext.RequestAborted))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "OAuth client реактивирован.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRotateSecretAsync(string id)
    {
        var result = await _clients.RotateSecretAsync(id, HttpContext.RequestAborted);
        if (result is null)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Client secret сменен. Скопируйте новый secret сейчас: он больше не будет показан.";
        TempData["CreatedClientId"] = result.ClientId;
        TempData["CreatedClientSecret"] = result.ClientSecret;
        return RedirectToPage(new { id });
    }
}
