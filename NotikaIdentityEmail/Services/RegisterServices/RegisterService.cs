using Microsoft.AspNetCore.Identity;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Services.EmailServices;

namespace NotikaIdentityEmail.Services.RegisterServices
{
    public class RegisterService : IRegisterService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public RegisterService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
        {
            // 1️⃣ Kullanıcıyı oluştur
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return result;
            }

            // 2️⃣ User rolü yoksa oluştur
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("User"));
                if (!roleResult.Succeeded)
                {
                    return roleResult;
                }
            }

            // 3️⃣ User rolünü ATA (REGISTER ANINDA)
            var roleAssignResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleAssignResult.Succeeded)
            {
                return roleAssignResult;
            }

            return IdentityResult.Success;
        }

        public async Task<int> AssignActivationCodeAsync(AppUser user)
        {
            var activationCode = Random.Shared.Next(100000, 999999);
            user.ActivationCode = activationCode;
            await _userManager.UpdateAsync(user);
            return activationCode;
        }

        public async Task SendActivationEmailAsync(string email, int activationCode)
        {
            await _emailService.SendAsync(
                email,
                "Notika | Hesap Aktivasyonu",
                $"<h2>Aktivasyon Kodunuz: {activationCode}</h2>");
        }
    }
}
