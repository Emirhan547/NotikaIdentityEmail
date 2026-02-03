using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize(Roles = "User")]
    public class MyProfileController : Controller
    {
        public IActionResult EditProfile()
        {
            return View();
        }
    }
}
