using Microsoft.AspNetCore.Identity;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Services;
using NotikaIdentityEmail.Services.EmailServices;

namespace NotikaIdentityEmail.Services.RegisterServices;

public class RegisterService : IRegisterService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;

    public RegisterService(UserManager<AppUser> userManager, IEmailService emailService)
    {
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
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