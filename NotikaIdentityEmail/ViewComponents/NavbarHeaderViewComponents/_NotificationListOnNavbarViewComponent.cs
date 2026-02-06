using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;

namespace NotikaIdentityEmail.ViewComponents.NavbarHeaderViewComponents
{
    public class _NotificationListOnNavbarViewComponent : ViewComponent
    {
        private readonly EmailContext _emailContext;

        public _NotificationListOnNavbarViewComponent(EmailContext emailContext)
        {
            _emailContext = emailContext;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var values = await _emailContext.Notifications
                .OrderByDescending(x => x.NotificationId)
                .Take(5)
                .ToListAsync();
            return View(values);
        }
    }
}