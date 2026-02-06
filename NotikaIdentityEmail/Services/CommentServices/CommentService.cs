using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.CommentServices
{
    public class CommentService:ICommentService
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CommentService(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<Comment>> GetUserCommentsAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return new List<Comment>();
            }

            return await _context.Comments
                .Include(x => x.AppUser)
                .Where(x => x.AppUserId == user.Id)
                .ToListAsync();
        }

        public async Task<bool> CreateCommentAsync(string username, Comment comment)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return false;
            }

            comment.AppUserId = user.Id;
            comment.CommentDate = DateTime.Now;
            comment.CommentStatus = "Onay Bekliyor";
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
