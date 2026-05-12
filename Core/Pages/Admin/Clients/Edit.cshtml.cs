using Core.Identity;
using Core.Models.Admin;
using Core.Services.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin.Clients;

[Authorize(Roles = ApplicationRoles.Admin)]
public class EditModel : PageModel
{
    private readonly IOAuthClientService _clients;

    public EditModel(IOAuthClientService clients)
    {
        _clients = clients;
    }

    [BindProperty]
    public OAuthClientEditViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var model = await _clients.GetEditModelAsync(id, HttpContext.RequestAborted);
        if (model is null)
        {
            return NotFound();
        }

        Input = model;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _clients.ValidateClientInput(Input, new ModelStateDictionaryAdapter(ModelState));
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await _clients.UpdateClientAsync(Input, HttpContext.RequestAborted))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "OAuth client обновлен.";
        return RedirectToPage("./Details", new { id = Input.Id });
    }
}
