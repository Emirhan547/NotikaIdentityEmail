using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.LoginServices
{
    public interface ILoginService
    {
        Task<AppUser?> FindUserByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> CheckPasswordSignInAsync(AppUser user, string password, bool rememberMe);
        Task<string?> ResolveRedirectActionAsync(AppUser user);
        Task SignOutAsync();
    }
}
