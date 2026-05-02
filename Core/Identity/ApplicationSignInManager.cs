using Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Core.Identity;

public class ApplicationSignInManager : SignInManager<ApplicationUser>
{
    public ApplicationSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    protected override async Task<SignInResult?> PreSignInCheck(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            return SignInResult.Failed;
        }

        return await base.PreSignInCheck(user);
    }
}
