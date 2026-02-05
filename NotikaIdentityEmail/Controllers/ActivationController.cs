using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;


namespace NotikaIdentityEmail.Controllers
{
    public class ActivationController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ActivationController> _logger;
        public ActivationController(UserManager<AppUser> userManager, ILogger<ActivationController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(ActivationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

           

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                using (_logger.BeginScope(BuildAuthScope()))
                {
                    _logger.LogWarning(LogMessages.UserNotFound);
                }

                ModelState.AddModelError("", "Kullanıcı bulunamadı");
                return View(model);
            }

            if (user.ActivationCode != model.Code)
            {
                using (_logger.BeginScope(BuildAuthScope(user.Email)))
                {
                    _logger.LogWarning(LogMessages.ActivationCodeInvalid);
                }

                ModelState.AddModelError("", "Aktivasyon kodu hatalı");
                return View(model);
            }

            user.EmailConfirmed = true;
            user.ActivationCode = null;

            await _userManager.UpdateAsync(user);

            using (_logger.BeginScope(BuildAuthScope(user.Email)))
            {
                _logger.LogInformation(AuthLogMessages.UserActivated);
            }

            return RedirectToAction("UserLogin", "Login");
        }
        private static Dictionary<string, object?> BuildAuthScope(string? userEmail = null)
        {
            var scope = new Dictionary<string, object?>
            {
                ["OperationType"] = LogContextValues.OperationAuth
            };

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                scope["UserEmail"] = userEmail;
            }

            return scope;
        }
    }
}