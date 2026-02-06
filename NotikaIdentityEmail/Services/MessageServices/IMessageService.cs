using Microsoft.AspNetCore.Mvc.Rendering;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Services.MessageServices
{
    public interface IMessageService
    {
        Task<List<MessageWithSenderInfoViewModel>> GetInboxAsync(string userEmail, string? query);
        Task<List<MessageWithReceiverInfoViewModel>> GetSendboxAsync(string userEmail, string? query);
        Task<List<MessageWithSenderInfoViewModel>> GetByCategoryAsync(string userEmail, int categoryId);
        Task<List<MessageWithReceiverInfoViewModel>> GetDraftsAsync(string userEmail, string? query);
        Task<List<MessageTrashViewModel>> GetTrashAsync(string userEmail, string? query);
        Task<Message?> GetMessageForUserAsync(int messageId, string userEmail);
        Task<bool> ReceiverExistsAsync(string receiverEmail);
        Task<Message> CreateMessageAsync(string senderEmail, ComposeMessageViewModel model, bool isDraft);
        Task<ComposeMessageViewModel?> BuildReplyModelAsync(int messageId, string userEmail);
        Task<ComposeMessageViewModel?> BuildForwardModelAsync(int messageId, string userEmail);
        Task<Message?> MoveToTrashAsync(int messageId, string userEmail);
        Task MarkAsReadAsync(Message message, string readerEmail);
        Task<List<SelectListItem>> GetCategorySelectListAsync();
        Task<string?> GetCategoryNameAsync(int categoryId);
    }
}
