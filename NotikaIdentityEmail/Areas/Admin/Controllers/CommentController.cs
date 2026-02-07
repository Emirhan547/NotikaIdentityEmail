using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Context;

namespace NotikaIdentityEmail.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CommentController : Controller
    {
        private readonly EmailContext _context;

        public CommentController(EmailContext context)
        {
            _context = context;
        }

        // 🔥 ADMIN LIST – SADECE DB OKUR (HF YOK)
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var comments = await _context.Comments
                .Include(x => x.AppUser)
                .OrderByDescending(x => x.CommentDate)
                .ToListAsync(cancellationToken);

            var model = comments.Select(comment => new AdminCommentViewModel
            {
                CommentId = comment.CommentId,
                Username = comment.AppUser?.UserName ?? "-",
                FullName = $"{comment.AppUser?.Name} {comment.AppUser?.Surname}".Trim(),
                CommentDetail = comment.CommentDetail,

                // 🔑 ARTIK NON-NULLABLE
                ToxicityLabel = comment.ToxicityLabel,
                ToxicityScore = comment.ToxicityScore,
                IsToxic = comment.IsToxic,

                Status = string.IsNullOrWhiteSpace(comment.CommentStatus)
         ? "Onay Bekliyor"
         : comment.CommentStatus,

                CommentDate = comment.CommentDate,
                Translation = null
            }).ToList();


            return View(model);
        }

        // 🔄 MANUEL AKTİF / PASİF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(x => x.CommentId == id, cancellationToken);

            if (comment == null)
                return NotFound();

            comment.CommentStatus =
                comment.CommentStatus == "Aktif"
                    ? "Pasif"
                    : "Aktif";

            await _context.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }
    }
}
