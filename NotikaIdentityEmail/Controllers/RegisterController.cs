using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Services;
using NotikaIdentityEmail.Services.RegisterServices;


namespace NotikaIdentityEmail.Controllers
{
    [AllowAnonymous]
    public class RegisterController : Controller
    {
        private readonly IRegisterService _registerService;
        private readonly ILogger<RegisterController> _logger;
        public RegisterController(IRegisterService registerService, ILogger<RegisterController> logger)
        {
            _registerService = registerService;
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
            {
                return View(model);
            }
        

            var user = new AppUser
            {
                UserName = model.Username,
                Email = model.Email,
                Name = model.Name,
                Surname = model.Surname
            };

            var result = await _registerService.CreateUserAsync(user, model.Password);

            if (!result.Succeeded)
            {
                using (_logger.BeginScope(BuildUserScope(LogContextValues.OperationUser, model.Email)))
                {
                    _logger.LogWarning(UserLogMessages.UserCreateFailed);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            var activationCode = await _registerService.AssignActivationCodeAsync(user);
            try
            {
                await _registerService.SendActivationEmailAsync(user.Email!, activationCode);

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