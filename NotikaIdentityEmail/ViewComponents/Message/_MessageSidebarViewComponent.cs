using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.ViewComponents.Message
{
    public class _MessageSidebarViewComponent : ViewComponent
    {
        private readonly EmailContext _emailContext;
        private readonly UserManager<AppUser> _userManager;

        public _MessageSidebarViewComponent(EmailContext emailContext, UserManager<AppUser> userManager)
        {
            _emailContext = emailContext;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            ViewBag.sendMessageCount = _emailContext.Messages.Count(x => x.SenderEmail == user.Email && !x.IsDeleted && !x.IsDraft);
            ViewBag.receiveMessageCount = _emailContext.Messages.Count(x => x.ReceiverEmail == user.Email && !x.IsDeleted && !x.IsDraft);
            ViewBag.unreadMessageCount = _emailContext.Messages.Count(x => x.ReceiverEmail == user.Email && !x.IsDeleted && !x.IsDraft && !x.IsRead);
            ViewBag.draftMessageCount = _emailContext.Messages.Count(x => x.SenderEmail == user.Email && x.IsDraft && !x.IsDeleted);
            ViewBag.trashMessageCount = _emailContext.Messages.Count(x => x.IsDeleted && (x.SenderEmail == user.Email || x.ReceiverEmail == user.Email));
            return View();
        }
    }
}
