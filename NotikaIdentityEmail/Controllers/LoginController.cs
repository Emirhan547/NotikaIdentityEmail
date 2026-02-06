using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Services.LoginServices;


namespace NotikaIdentityEmail.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
       
        private readonly ILoginService _loginService;
        private readonly ILogger<LoginController> _logger;
        

        public LoginController(ILoginService loginService, ILogger<LoginController> logger)
        {
           
            _loginService = loginService;
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

           
            var user = await _loginService.FindUserByUsernameOrEmailAsync(model.Username);

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

           
                var isSuccess = await _loginService.CheckPasswordSignInAsync(user, model.Password, model.RememberMe);
            if (!isSuccess)
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

           
                var target = await _loginService.ResolveRedirectActionAsync(user);
            if (target == "AdminDashboard")
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

           
                if (target == "UserInbox")
                {
                    return RedirectToAction("Inbox", "Message");
                }

          
            using (_logger.BeginScope(BuildAuthScope(user.Email)))
            {
                _logger.LogWarning(LogMessages.UserRoleMissing);
            }
          

            await _loginService.SignOutAsync();
            ModelState.AddModelError("", "Kullanıcı rolü tanımlı değil");
            return View(model);
        }

        // 🚪 LOGOUT
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;
            await _loginService.SignOutAsync();

            {               
                    _logger.LogInformation(AuthLogMessages.UserLogout);
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
