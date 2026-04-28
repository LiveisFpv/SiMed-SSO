using Microsoft.AspNetCore.Identity;

namespace Core.Models;

// Будущая сущнсоть пользователя SSO системы
public class ApplicationUser: IdentityUser
{
    public DateTime CreatedAtUtc {get; set;} = DateTime.UtcNow;
    public bool IsActive {get; set;} = true;
}