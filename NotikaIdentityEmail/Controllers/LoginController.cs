using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using Serilog;

namespace NotikaIdentityEmail.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // 🔐 LOGIN GET
        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        // 🔐 LOGIN POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserLogin(UserLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Username)
                       ?? await _userManager.FindByEmailAsync(model.Username);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationAuth)
                    .Warning(AuthLogMessages.UserLoginFailed);

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                Log.ForContext("OperationType", LogContextValues.OperationAuth)
                     .ForContext("UserEmail", user.Email)
                     .Warning(AuthLogMessages.UserLoginFailed);

                ModelState.AddModelError("", "Email adresiniz doğrulanmamış");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (!result.Succeeded)
            {
                Log.ForContext("OperationType", LogContextValues.OperationAuth)
                    .ForContext("UserEmail", user.Email)
                    .Warning(AuthLogMessages.UserLoginFailed);

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                return View(model);
            }

            Log.ForContext("OperationType", LogContextValues.OperationAuth)
                .ForContext("UserEmail", user.Email)
                .Information(AuthLogMessages.UserLoginSuccess);

            // 🎯 ROLE BASED REDIRECT
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                return RedirectToAction("Inbox", "Message");
            }

            // ⚠️ Rolü olmayan kullanıcı
            Log.ForContext("OperationType", LogContextValues.OperationAuth)
                .ForContext("UserEmail", user.Email)
                .Warning(LogMessages.UserRoleMissing);
            await _signInManager.SignOutAsync();
            ModelState.AddModelError("", "Kullanıcı rolü tanımlı değil");
            return View(model);
        }

        // 🚪 LOGOUT
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            var log = Log.ForContext("OperationType", LogContextValues.OperationAuth);
            if (!string.IsNullOrWhiteSpace(username))
            {
                log = log.ForContext("UserEmail", username);
            }

            log.Information(AuthLogMessages.UserLogout);

            return RedirectToAction("UserLogin");
        }
    }
}