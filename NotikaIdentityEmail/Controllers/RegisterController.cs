using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Services;


namespace NotikaIdentityEmail.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterController> _logger;
        public RegisterController(
            UserManager<AppUser> userManager,
           IEmailService emailService,
            ILogger<RegisterController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
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
                using (_logger.BeginScope(BuildUserScope(LogContextValues.OperationUser, model.Email)))
                {
                    _logger.LogWarning(UserLogMessages.UserCreateFailed);
                }

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
                using (_logger.BeginScope(BuildUserScope(LogContextValues.OperationSystem, user.Email)))
                {
                    _logger.LogError(ex, LogMessages.ActivationEmailFailed);
                }

                TempData["WarningMessage"] =
                    "Hesabınız oluşturuldu ancak aktivasyon e-postası gönderilemedi. " +
                    $"Aktivasyon kodunuz: {activationCode}";
            }
            using (_logger.BeginScope(BuildUserScope(LogContextValues.OperationUser, user.Email)))
            {
                _logger.LogInformation(UserLogMessages.UserCreated);
            }


            return RedirectToAction("Index", "Activation");
        }
        private static Dictionary<string, object?> BuildUserScope(string operationType, string? userEmail = null)
        {
            var scope = new Dictionary<string, object?>
            {
                ["OperationType"] = operationType
            };

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                scope["UserEmail"] = userEmail;
            }

            return scope;
        }
    }
}