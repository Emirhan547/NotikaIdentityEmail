using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.ViewComponents
{
    public class _UILayoutHeaderViewComponent : ViewComponent
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        public _UILayoutHeaderViewComponent(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userValue = await _userManager.FindByNameAsync(User.Identity?.Name);
            if (userValue != null)
            {
                var userEmail = userValue.Email;
                ViewBag.unreadMessageCount = _context.Messages.Count(x => x.ReceiverEmail == userEmail && !x.IsDeleted && !x.IsDraft && !x.IsRead);
                ViewBag.notificationCount = _context.Notifications.Count();
            }
            else
            {
                ViewBag.unreadMessageCount = 0;
                ViewBag.notificationCount = 0;
            }
            return View();
        }
    }
}