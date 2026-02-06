using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.ViewComponents.NavbarHeaderViewComponents
{
    public class _MessageListOnNavbarViewComponent : ViewComponent
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailContext _context;

        public _MessageListOnNavbarViewComponent(UserManager<AppUser> userManager, EmailContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userValue = await _userManager.FindByNameAsync(User.Identity?.Name);
            if (userValue == null)
            {
                return View(new List<MessageListWithUsersInfoViewModel>());
            }
            var userEmail = userValue.Email;
            var values = await (from message in _context.Messages
                                join user in _context.Users on message.SenderEmail equals user.Email
                                where message.ReceiverEmail == userEmail && !message.IsDeleted && !message.IsDraft
                                orderby message.SendDate descending
                                select new MessageListWithUsersInfoViewModel
                                {
                                    MessageId = message.MessageId,
                                    FullName = (user.Name + " " + user.Surname).Trim(),
                                    ProfileImageUrl = user.ImageUrl,
                                    Subject = message.Subject,
                                    SendDate = message.SendDate,
                                    MessageDetail = message.MessageDetail,
                                    IsRead = message.IsRead
                                })
                                  .Take(5)
                                  .ToListAsync();

            return View(values);
        }

    }
}
