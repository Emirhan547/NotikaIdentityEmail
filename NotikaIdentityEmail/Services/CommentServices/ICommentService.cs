using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.CommentServices
{
    public interface ICommentService
    {
        Task<List<Comment>> GetUserCommentsAsync(string username);
        Task<bool> CreateCommentAsync(string username, Comment comment);
    }
}
