using Microsoft.AspNetCore.Identity;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.RegisterServices
{
    public interface IRegisterService
    {
        Task<IdentityResult> CreateUserAsync(AppUser user, string password);
        Task<int> AssignActivationCodeAsync(AppUser user);
        Task SendActivationEmailAsync(string email, int activationCode);
    }
}
