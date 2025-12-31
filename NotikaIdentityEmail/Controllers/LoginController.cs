using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Controllers
{
    public class LoginController : Controller
    {

        private readonly SignInManager<AppUser> _signInManager;
        private readonly EmailContext _emailContext;
        public LoginController(SignInManager<AppUser> signInManager, EmailContext emailContext)
        {
            _signInManager = signInManager;
            _emailContext = emailContext;
        }

        public IActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginViewModel model)
        {
            var value=_emailContext.Users.Where(x=>x.UserName==model.Username).FirstOrDefault();
            if (value.EmailConfirmed==true)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Profile", "EditProfile");
                }
                return View();
            }           
            return View();
        }
    }
}