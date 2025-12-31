using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.ViewComponents._UILayout
{
    public class _UILayoutHeaderViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
