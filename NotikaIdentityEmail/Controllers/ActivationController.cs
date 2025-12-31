using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;

namespace NotikaIdentityEmail.Controllers
{
    public class ActivationController : Controller
    {
        private readonly EmailContext _emailContext;

        public ActivationController(EmailContext emailContext)
        {
            _emailContext = emailContext;
        }
        [HttpGet]
        public IActionResult UserActivation()
        {
            TempData.Keep("EmailMove"); // 👈 önemli
            TempData["Test1"] = TempData["EmailMove"];
            return View();
        }


        [HttpPost]
        public IActionResult UserActivation(int userCodeParameter)
        {
            var emailObj = TempData.Peek("Test1"); // 👈 silmez

            if (emailObj == null)
            {
                ModelState.AddModelError("", "Oturum süresi doldu.");
                return View();
            }

            string email = emailObj.ToString();

            var user = _emailContext.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
                return View();
            }

            if (userCodeParameter == user.ActivationCode)
            {
                user.EmailConfirmed = true;
                _emailContext.SaveChanges();
                return RedirectToAction("UserLogin", "Login");
            }

            ModelState.AddModelError("", "Aktivasyon kodu hatalı.");
            return View();
        }


    }
}
