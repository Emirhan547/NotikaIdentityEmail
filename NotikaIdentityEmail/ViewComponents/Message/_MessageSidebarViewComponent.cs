using Microsoft.AspNetCore.Mvc;

namespace NotikaIdentityEmail.ViewComponents.Message
{
    public class _MessageSidebarViewComponent:ViewComponent
    { 
        public IViewComponentResult Invoke ()
        {
            return View();
        }
    }
}
