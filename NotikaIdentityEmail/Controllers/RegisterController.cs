using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Services;
using Serilog;

namespace NotikaIdentityEmail.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public RegisterController(
            UserManager<AppUser> userManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Log.Information(
                LogMessages.UserRegisterStarted,
                model.Email,
                model.Username
            );

            var user = new AppUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));

                Log.Warning(
                    LogMessages.UserRegisterFailed,
                    model.Email,
                    errors
                );

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            var activationCode = new Random().Next(100000, 999999);
            user.ActivationCode = activationCode;

            await _userManager.UpdateAsync(user);

            try
            {
                await _emailService.SendAsync(
                    user.Email!,
                    "Notika | Hesap Aktivasyonu",
                    $"<h2>Aktivasyon Kodunuz: {activationCode}</h2>"
                );
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Activation email failed after user creation. UserId: {UserId}, Email: {Email}",
                    user.Id,
                    user.Email
                );
            }

            Log.Information(
                LogMessages.UserRegisterSucceeded,
                user.Id,
                user.Email
            );

            return RedirectToAction("Index", "Activation");
        }
    }
}
