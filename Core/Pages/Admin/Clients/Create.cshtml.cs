using Core.Identity;
using Core.Models;
using Core.Models.Admin;
using Core.Services.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Core.Pages.Admin.Clients;

[Authorize(Roles = ApplicationRoles.Admin)]
public class CreateModel : PageModel
{
    private readonly IOAuthClientService _clients;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(
        IOAuthClientService clients,
        UserManager<ApplicationUser> userManager)
    {
        _clients = clients;
        _userManager = userManager;
    }

    [BindProperty]
    public OAuthClientCreateViewModel Input { get; set; } = new();

    public void OnGet()
    {
        Input = _clients.CreateEmptyCreateModel();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _clients.ValidateClientInput(Input, new ModelStateDictionaryAdapter(ModelState));
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _clients.CreateClientAsync(
            Input,
            _userManager.GetUserId(User),
            HttpContext.RequestAborted);

        TempData["StatusMessage"] = "OAuth client создан. Скопируйте client secret сейчас: он больше не будет показан.";
        TempData["CreatedClientId"] = result.ClientId;
        TempData["CreatedClientSecret"] = result.ClientSecret;
        return RedirectToPage("./Details", new { id = result.Id });
    }
}
