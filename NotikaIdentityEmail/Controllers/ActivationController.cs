using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;

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
        public IActionResult UserActivation()
        {
            if (!TempData.ContainsKey("Email"))
                return RedirectToAction("CreateUser", "Register");

            TempData.Keep("Email"); 
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UserActivation(int userCodeParameter)
        {
            var email = TempData["Email"]?.ToString();

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Oturum süresi doldu. Lütfen tekrar kayıt olun.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                return View();
            }

            if (user.EmailConfirmed)
            {
                return RedirectToAction("UserLogin", "Login");
            }

            if (user.ActivationCode != userCodeParameter)
            {
                TempData.Keep("Email"); 
                ModelState.AddModelError("", "Aktivasyon kodu hatalı.");
                return View();
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            TempData.Remove("Email");
            return RedirectToAction("UserLogin", "Login");
        }
    }
}
