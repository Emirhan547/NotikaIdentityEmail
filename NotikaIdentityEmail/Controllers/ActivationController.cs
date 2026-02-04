using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using Serilog;

namespace NotikaIdentityEmail.Controllers
{
    public class ActivationController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ActivationController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
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

            Log.Information(
                LogMessages.ActivationStarted,
                model.Email,
                model.Code
            );

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                Log.Warning(
                    LogMessages.ActivationFailedUserNotFound,
                    model.Email
                );

                ModelState.AddModelError("", "Kullanıcı bulunamadı");
                return View(model);
            }

            if (user.ActivationCode != model.Code)
            {
                Log.Warning(
                    LogMessages.ActivationFailedWrongCode,
                    model.Email
                );

                ModelState.AddModelError("", "Aktivasyon kodu hatalı");
                return View(model);
            }

            user.EmailConfirmed = true;
            user.ActivationCode = null;

            await _userManager.UpdateAsync(user);

            Log.Information(
                LogMessages.ActivationSucceeded,
                user.Id,
                user.Email
            );

            return RedirectToAction("UserLogin", "Login");
        }
    }
}
