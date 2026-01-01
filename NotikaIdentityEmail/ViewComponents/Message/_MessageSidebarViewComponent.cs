using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using System.Threading.Tasks;

namespace NotikaIdentityEmail.ViewComponents.Message
{
    public class _MessageSidebarViewComponent:ViewComponent
    { 
        private readonly EmailContext _emailContext;
        private readonly UserManager<AppUser>_userManager;

        public _MessageSidebarViewComponent(EmailContext emailContext, UserManager<AppUser> userManager)
        {
            _emailContext = emailContext;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> Invoke ()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            ViewBag.sendMessageCount = _emailContext.Messages.Where(x => x.SenderEmail == user.Email).Count();
            ViewBag.receiveMessageCount = _emailContext.Messages.Where(x => x.ReceiverMail == user.Email).Count();
            return View();
        }
    }
}
