using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.Controllers
{
    public class MyProfileController : Controller
    {
        public IActionResult EditProfile()
        {
            return View();
        }
    }
}
