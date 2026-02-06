using Microsoft.AspNetCore.Identity;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.LoginServices
{
    public class LoginService:ILoginService
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginService(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<AppUser?> FindUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _userManager.FindByNameAsync(usernameOrEmail)
                   ?? await _userManager.FindByEmailAsync(usernameOrEmail);
        }

        public async Task<bool> CheckPasswordSignInAsync(AppUser user, string password, bool rememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
            return result.Succeeded;
        }

        public async Task<string?> ResolveRedirectActionAsync(AppUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return "AdminDashboard";
            }

            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                return "UserInbox";
            }

            return null;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
