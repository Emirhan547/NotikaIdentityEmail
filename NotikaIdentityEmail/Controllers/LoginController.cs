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

        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
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
                Log.Warning(
                    LogMessages.LoginFailed,
                    model.Username
                );

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                Log.Warning(
                    "Login blocked - email not confirmed. UserId: {UserId}, Email: {Email}",
                    user.Id,
                    user.Email
                );

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
                Log.Warning(
                    LogMessages.LoginFailed,
                    model.Username
                );

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                return View(model);
            }

            Log.Information(
                LogMessages.LoginSucceeded,
                user.Id,
                user.UserName
            );

            return RedirectToAction("Index", "Inbox");
        }

        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            Log.Information(
                "User logged out. Username: {Username}",
                username
            );

            return RedirectToAction("UserLogin");
        }
    }
}
