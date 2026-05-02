using Microsoft.AspNetCore.Mvc;

namespace Core.Controllers;

public class AccountController : Controller
{
    public IActionResult AccessDenied()
    {
        return View();
    }
}
