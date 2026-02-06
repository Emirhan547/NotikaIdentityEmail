using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Services.CommentServices;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize(Roles = "User")]
    public class CommentController : Controller
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }
        public async Task<IActionResult> UserComments()
        {
            var comments = await _commentService.GetUserCommentsAsync(User.Identity!.Name!);
            return View(comments);
        }

        public async Task<IActionResult> UserCommentList()
        {
            var comments = await _commentService.GetUserCommentsAsync(User.Identity!.Name!);
            return View(comments);
        }

        [HttpGet]
        public PartialViewResult CreateComment()
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(Comment comment)
        {
            var created = await _commentService.CreateCommentAsync(User.Identity!.Name!, comment);
            if (!created)
            {
                return Unauthorized();
            }
            return RedirectToAction("UserCommentList");
        }
    }
}
 