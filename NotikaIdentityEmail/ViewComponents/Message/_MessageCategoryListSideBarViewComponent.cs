
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.ViewComponents.Message
{
    public class _MessageCategoryListSideBarViewComponent : ViewComponent
    {
        private readonly EmailContext _emailContext;
        private readonly UserManager<AppUser> _userManager;
        public _MessageCategoryListSideBarViewComponent(EmailContext emailContext, UserManager<AppUser> userManager)
        {
            _emailContext = emailContext;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? string.Empty);
            var userEmail = user?.Email;

            var categoryList = await _emailContext.Categories
                .OrderBy(x => x.CategoryName)
                .Select(x => new CategorySidebarItemViewModel
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    CategoryIconUrl = x.CategoryIconUrl,
                    MessageCount = userEmail == null
                        ? 0
                        : _emailContext.Messages.Count(m =>
                            m.CategoryId == x.CategoryId &&
                            m.ReceiverEmail == userEmail &&
                            !m.IsDeleted &&
                            !m.IsDraft)
                })
                .ToListAsync();

            return View(categoryList);
        }
    }
}
