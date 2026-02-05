using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<LoginController> _logger;
        public LoginController(
            SignInManager<AppUser> signInManager,
           UserManager<AppUser> userManager,
            ILogger<LoginController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
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
                using (_logger.BeginScope(BuildAuthScope()))
                {
                    _logger.LogWarning(AuthLogMessages.UserLoginFailed);
                }

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                using (_logger.BeginScope(BuildAuthScope(user.Email)))
                {
                    _logger.LogWarning(AuthLogMessages.UserLoginFailed);
                }

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
                using (_logger.BeginScope(BuildAuthScope(user.Email)))
                {
                    _logger.LogWarning(AuthLogMessages.UserLoginFailed);
                }

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                return View(model);
            }

            using (_logger.BeginScope(BuildAuthScope(user.Email)))
            {
                _logger.LogInformation(AuthLogMessages.UserLoginSuccess);
            }

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
            using (_logger.BeginScope(BuildAuthScope(user.Email)))
            {
                _logger.LogWarning(LogMessages.UserRoleMissing);
            }
            await _signInManager.SignOutAsync();
            ModelState.AddModelError("", "Kullanıcı rolü tanımlı değil");
            return View(model);
        }

        // 🚪 LOGOUT
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            if (!string.IsNullOrWhiteSpace(username))
            {
                using (_logger.BeginScope(BuildAuthScope(username)))
                {
                    _logger.LogInformation(AuthLogMessages.UserLogout);
                }
            }
            else
            {
                using (_logger.BeginScope(BuildAuthScope()))
                {
                    _logger.LogInformation(AuthLogMessages.UserLogout);
                }
            }

           

            return RedirectToAction("UserLogin");
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