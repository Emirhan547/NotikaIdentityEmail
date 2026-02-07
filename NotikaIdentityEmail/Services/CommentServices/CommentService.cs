using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Services.CommentServices;
using NotikaIdentityEmail.Services.HuggingFaces;

public class CommentService : ICommentService
{
    private readonly EmailContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHuggingFaceService _huggingFaceService;

    public CommentService(
        EmailContext context,
        UserManager<AppUser> userManager,
        IHuggingFaceService huggingFaceService)
    {
        _context = context;
        _userManager = userManager;
        _huggingFaceService = huggingFaceService;
    }

    public async Task<bool> CreateCommentAsync(string username, Comment comment)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return false;

        var analysis = await _huggingFaceService
            .AnalyzeToxicityAsync(comment.CommentDetail);

        comment.AppUserId = user.Id;
        comment.CommentDate = DateTime.Now;

        // 🔑 NULL SAFE – ENTITY NON-NULLABLE
        comment.IsToxic = analysis != null && analysis.IsToxic;
        comment.ToxicityScore = analysis?.Score ?? 0;
        comment.ToxicityLabel = analysis?.Label ?? "unknown";

        comment.CommentStatus = comment.IsToxic
            ? "Pasif"
            : "Onay Bekliyor";

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return true;
    }


    public async Task<List<Comment>> GetUserCommentsAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
            return new List<Comment>();

        return await _context.Comments
            .Where(x => x.AppUserId == user.Id)
            .OrderByDescending(x => x.CommentDate)
            .ToListAsync();
    }
}
