using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize(Roles = "User")]
    public class CommentController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        public CommentController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> UserComments()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var values = await _context.Comments
                .Include(x => x.AppUser)
                .Where(x => x.AppUserId == user.Id)
                .ToListAsync();
            return View(values);
        }

        public async Task<IActionResult> UserCommentList()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var values = await _context.Comments
                .Include(x => x.AppUser)
                .Where(x => x.AppUserId == user.Id)
                .ToListAsync();
            return View(values);
        }

        [HttpGet]
        public PartialViewResult CreateComment() 
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(Comment comment)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            comment.AppUserId = user.Id;
            comment.CommentDate = DateTime.Now;
            comment.CommentStatus = "Onay Bekliyor";
            _context.Comments.Add(comment);
            _context.SaveChanges();
            return RedirectToAction("UserCommentList");
        }
    }
}
 