using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.ViewComponents._UILayout
{
    public class _UILayoutHeadViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
