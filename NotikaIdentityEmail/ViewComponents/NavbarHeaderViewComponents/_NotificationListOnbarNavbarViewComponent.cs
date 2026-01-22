using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;

namespace NotikaIdentityEmail.ViewComponents.NavbarHeaderViewComponents
{
    public class _NotificationListOnbarNavbarViewComponent:ViewComponent
    {
        private readonly EmailContext _emailContext;

        public _NotificationListOnbarNavbarViewComponent(EmailContext emailContext)
        {
            _emailContext = emailContext;
        }
        public IViewComponentResult Invoke()
        {
            var values=_emailContext.Notifications.ToList();
            return View(values);
        }
    }
}
