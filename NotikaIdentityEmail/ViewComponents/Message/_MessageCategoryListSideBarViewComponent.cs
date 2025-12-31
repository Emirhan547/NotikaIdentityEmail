using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Context;

namespace NotikaIdentityEmail.ViewComponents.Message
{
    public class _MessageCategoryListSideBarViewComponent:ViewComponent
    {
        private readonly EmailContext _emailContext;

        public _MessageCategoryListSideBarViewComponent(EmailContext emailContext)
        {
            _emailContext = emailContext;
        }

        public IViewComponentResult Invoke()
        {
            var values=_emailContext.Categories.ToList();
            return View(values);
        }
    }
}
