using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using System.Security.Claims;

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
            var user = HttpContext.User;

            var userEmail =
                user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue("email")
                ?? user.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return View(new List<Entities.Notification>());
            }

            var values = await _emailContext.Notifications
                .Where(x => x.RecipientEmail == userEmail)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(values);
        }
    }
}
