using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.ViewComponents._UILayout
{
    public class _UILayoutNavbarViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke ()
        {
            return View();
        }
    }
}
