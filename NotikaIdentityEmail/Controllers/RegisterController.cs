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

        

            var user = new AppUser
            {
                UserName = model.Username,
                Email = model.Email,
                Name = model.Name,
                Surname = model.Surname
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                Log.ForContext("OperationType", LogContextValues.OperationUser)
                     .ForContext("UserEmail", model.Email)
                     .Warning(UserLogMessages.UserCreateFailed);

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            var activationCode = new Random().Next(100000, 999999);
            user.ActivationCode = activationCode;

            await _userManager.UpdateAsync(user);

            // ✅ DÜZELTME: Email hatası kullanıcıya bildirilir
            try
            {
                await _emailService.SendAsync(
                    user.Email!,
                    "Notika | Hesap Aktivasyonu",
                    $"<h2>Aktivasyon Kodunuz: {activationCode}</h2>"
                );

                TempData["SuccessMessage"] = "Aktivasyon kodu e-posta adresinize gönderildi.";
            }
            catch (Exception ex)
            {
                Log.ForContext("OperationType", LogContextValues.OperationSystem)
                     .ForContext("UserEmail", user.Email)
                     .Error(ex, LogMessages.ActivationEmailFailed);

                TempData["WarningMessage"] =
                    "Hesabınız oluşturuldu ancak aktivasyon e-postası gönderilemedi. " +
                    $"Aktivasyon kodunuz: {activationCode}";
            }
            Log.ForContext("OperationType", LogContextValues.OperationUser)
                .ForContext("UserEmail", user.Email)
                .Information(UserLogMessages.UserCreated);


            return RedirectToAction("Index", "Activation");
        }
    }
}