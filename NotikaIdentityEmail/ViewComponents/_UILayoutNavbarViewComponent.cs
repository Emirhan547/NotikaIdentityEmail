using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.ViewComponents
{
    public class _UILayoutNavbarViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke ()
        {
            return View();
        }
    }
}
