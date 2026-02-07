using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Hubs;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Services.MessageServices
{
    public class MessageService : IMessageService
    {
        private readonly EmailContext _context;
        private readonly IHtmlSanitizerService _htmlSanitizer;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            EmailContext context,
            IHtmlSanitizerService htmlSanitizer,
            IHubContext<NotificationHub> hubContext,
            ILogger<MessageService> logger)
        {
            _context = context;
            _htmlSanitizer = htmlSanitizer;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<List<MessageWithSenderInfoViewModel>> GetInboxAsync(string userEmail, string? query)
        {
            var values = from m in _context.Messages
                         join sender in _context.Users on m.SenderEmail equals sender.Email
                         join category in _context.Categories on m.CategoryId equals category.CategoryId
                         where m.ReceiverEmail == userEmail && !m.IsDeleted && !m.IsDraft
                         select new MessageWithSenderInfoViewModel
                         {
                             MessageId = m.MessageId,
                             MessageDetail = m.MessageDetail,
                             Subject = m.Subject,
                             SendDate = m.SendDate,
                             SenderEmail = m.SenderEmail,
                             SenderName = sender.Name,
                             SenderSurname = sender.Surname,
                             CategoryName = category.CategoryName,
                             IsRead = m.IsRead
                         };

            if (!string.IsNullOrWhiteSpace(query))
            {
                values = values.Where(x =>
                    x.Subject.Contains(query) ||
                    x.MessageDetail.Contains(query) ||
                    x.SenderEmail.Contains(query) ||
                    x.SenderName.Contains(query) ||
                    x.SenderSurname.Contains(query));
            }

            return await values.ToListAsync();
        }

        public async Task<List<MessageWithReceiverInfoViewModel>> GetSendboxAsync(string userEmail, string? query)
        {
            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.ReceiverEmail equals u.Email into userGroup
                         from receiver in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.SenderEmail == userEmail && !m.IsDeleted && !m.IsDraft
                         select new MessageWithReceiverInfoViewModel
                         {
                             MessageId = m.MessageId,
                             MessageDetail = m.MessageDetail,
                             Subject = m.Subject,
                             SendDate = m.SendDate,
                             ReceiverEmail = m.ReceiverEmail,
                             ReceiverName = receiver != null ? receiver.Name : "Bilinmeyen",
                             ReceiverSurname = receiver != null ? receiver.Surname : "Kullanıcı",
                             CategoryName = category != null ? category.CategoryName : "Kategori Yok"
                         };

            if (!string.IsNullOrWhiteSpace(query))
            {
                values = values.Where(x =>
                    x.Subject.Contains(query) ||
                    x.MessageDetail.Contains(query) ||
                    x.ReceiverEmail.Contains(query) ||
                    x.ReceiverName.Contains(query) ||
                    x.ReceiverSurname.Contains(query));
            }

            return await values.ToListAsync();
        }

        public async Task<List<MessageWithSenderInfoViewModel>> GetByCategoryAsync(string userEmail, int categoryId)
        {
            return await (from m in _context.Messages
                          join u in _context.Users
                          on m.SenderEmail equals u.Email into userGroup
                          from sender in userGroup.DefaultIfEmpty()

                          join c in _context.Categories
                          on m.CategoryId equals c.CategoryId into categoryGroup
                          from category in categoryGroup.DefaultIfEmpty()

                          where m.ReceiverEmail == userEmail && m.CategoryId == categoryId && !m.IsDeleted && !m.IsDraft
                          select new MessageWithSenderInfoViewModel
                          {
                              MessageId = m.MessageId,
                              MessageDetail = m.MessageDetail,
                              Subject = m.Subject,
                              SendDate = m.SendDate,
                              SenderEmail = m.SenderEmail,
                              SenderName = sender != null ? sender.Name : "Bilinmeyen",
                              SenderSurname = sender != null ? sender.Surname : "Kullanıcı",
                              CategoryName = category != null ? category.CategoryName : "Kategori Yok",
                              IsRead = m.IsRead
                          }).ToListAsync();
        }

        public async Task<List<MessageWithReceiverInfoViewModel>> GetDraftsAsync(string userEmail, string? query)
        {
            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.ReceiverEmail equals u.Email into userGroup
                         from receiver in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.SenderEmail == userEmail && m.IsDraft && !m.IsDeleted
                         select new MessageWithReceiverInfoViewModel
                         {
                             MessageId = m.MessageId,
                             MessageDetail = m.MessageDetail,
                             Subject = m.Subject,
                             SendDate = m.SendDate,
                             ReceiverEmail = m.ReceiverEmail,
                             ReceiverName = receiver != null ? receiver.Name : "Bilinmeyen",
                             ReceiverSurname = receiver != null ? receiver.Surname : "Kullanıcı",
                             CategoryName = category != null ? category.CategoryName : "Kategori Yok"
                         };

            if (!string.IsNullOrWhiteSpace(query))
            {
                values = values.Where(x =>
                    x.Subject.Contains(query) ||
                    x.MessageDetail.Contains(query) ||
                    x.ReceiverEmail.Contains(query) ||
                    x.ReceiverName.Contains(query) ||
                    x.ReceiverSurname.Contains(query));
            }

            return await values.ToListAsync();
        }

        public async Task<List<MessageTrashViewModel>> GetTrashAsync(string userEmail, string? query)
        {
            var values = from m in _context.Messages
                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.IsDeleted && (m.SenderEmail == userEmail || m.ReceiverEmail == userEmail)
                         select new MessageTrashViewModel
                         {
                             MessageId = m.MessageId,
                             Subject = m.Subject,
                             SenderEmail = m.SenderEmail,
                             ReceiverEmail = m.ReceiverEmail,
                             CategoryName = category != null ? category.CategoryName : "Kategori Yok",
                             SendDate = m.SendDate,
                             IsRead = m.IsRead
                         };

            if (!string.IsNullOrWhiteSpace(query))
            {
                values = values.Where(x =>
                    x.Subject.Contains(query) ||
                    x.SenderEmail.Contains(query) ||
                    x.ReceiverEmail.Contains(query) ||
                    x.CategoryName.Contains(query));
            }

            return await values.ToListAsync();
        }

        public async Task<Message?> GetMessageForUserAsync(int messageId, string userEmail)
        {
            return await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == messageId &&
                !x.IsDeleted &&
                (x.SenderEmail == userEmail || x.ReceiverEmail == userEmail));
        }

        public async Task<bool> ReceiverExistsAsync(string receiverEmail)
        {
            return await _context.Users.AnyAsync(x => x.Email == receiverEmail);
        }

        public async Task<Message> CreateMessageAsync(string senderEmail, ComposeMessageViewModel model, bool isDraft)
        {
            var message = new Message
            {
                SenderEmail = senderEmail,
                ReceiverEmail = model.ReceiverEmail,
                Subject = model.Subject,
                MessageDetail = _htmlSanitizer.Sanitize(model.MessageDetail),
                SendDate = DateTime.Now,
                IsRead = false,
                IsDraft = isDraft,
                IsDeleted = false,
                CategoryId = model.CategoryId ?? 0
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var categoryName = await GetCategoryNameAsync(message.CategoryId);
            var status = GetMessageStatus(message);
            using (BeginMessageScope(message.SenderEmail, message.ReceiverEmail, categoryName, status))
            {
                _logger.LogInformation(message.IsDraft ? MessageLogMessages.MessageDraftSaved : MessageLogMessages.MessageSent);
            }

            if (!message.IsDraft)
            {
                await _hubContext.Clients
                    .Group(message.ReceiverEmail)
                    .SendAsync("NewMessage", new
                    {
                        senderEmail = message.SenderEmail,
                        receiverEmail = message.ReceiverEmail,
                        subject = message.Subject,
                        sendDate = message.SendDate.ToString("dd.MM.yyyy HH:mm"),
                        messageId = message.MessageId
                    });
                var receiverNotification = await CreateNotificationAsync(
                   title: "Yeni Mesaj",
                   detail: $"{message.SenderEmail} size \"{message.Subject}\" mesajı gönderdi.",
                   recipientEmail: message.ReceiverEmail,
                   recipientRole: null);

                await _hubContext.Clients
                    .Group(message.ReceiverEmail)
                    .SendAsync("NewNotification", new
                    {
                        title = receiverNotification.Title,
                        detail = receiverNotification.Detail,
                        imageUrl = receiverNotification.ImageUrl,
                        createdAt = receiverNotification.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                    });

                var adminNotification = await CreateNotificationAsync(
                    title: "Yeni Mesaj Trafiği",
                    detail: $"{message.SenderEmail} → {message.ReceiverEmail}: {message.Subject}",
                    recipientEmail: null,
                    recipientRole: "Admin");

                await _hubContext.Clients
                    .Group("admins")
                    .SendAsync("NewNotification", new
                    {
                        title = adminNotification.Title,
                        detail = adminNotification.Detail,
                        imageUrl = adminNotification.ImageUrl,
                        createdAt = adminNotification.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                    });
            }

            return message;
        }

        public async Task<ComposeMessageViewModel?> BuildReplyModelAsync(int messageId, string userEmail)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == messageId && x.ReceiverEmail == userEmail && !x.IsDeleted);

            if (message == null)
            {
                return null;
            }

            return new ComposeMessageViewModel
            {
                ReceiverEmail = message.SenderEmail,
                Subject = $"Re: {message.Subject}"
            };
        }

        public async Task<ComposeMessageViewModel?> BuildForwardModelAsync(int messageId, string userEmail)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == messageId &&
                (x.ReceiverEmail == userEmail || x.SenderEmail == userEmail) &&
                !x.IsDeleted);

            if (message == null)
            {
                return null;
            }

            return new ComposeMessageViewModel
            {
                Subject = $"Fwd: {message.Subject}",
                MessageDetail = message.MessageDetail
            };
        }

        public async Task<Message?> MoveToTrashAsync(int messageId, string userEmail)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == messageId &&
                !x.IsDeleted &&
                (x.SenderEmail == userEmail || x.ReceiverEmail == userEmail));

            if (message == null)
            {
                return null;
            }

            message.IsDeleted = true;
            message.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var categoryName = await GetCategoryNameAsync(message.CategoryId);
            using (BeginMessageScope(message.SenderEmail, message.ReceiverEmail, categoryName, LogContextValues.MessageStatusTrash))
            {
                _logger.LogInformation(MessageLogMessages.MessageMovedToTrash);
            }

            return message;
        }

        public async Task MarkAsReadAsync(Message message, string readerEmail)
        {
            if (message.IsRead || message.ReceiverEmail != readerEmail)
            {
                return;
            }

            message.IsRead = true;
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(message.SenderEmail)
                .SendAsync("MessageRead", new
                {
                    messageId = message.MessageId,
                    readerEmail,
                    subject = message.Subject
                });
            var senderNotification = await CreateNotificationAsync(
                title: "Mesaj Okundu",
                detail: $"{readerEmail}, \"{message.Subject}\" mesajını okudu.",
                recipientEmail: message.SenderEmail,
                recipientRole: null);

            await _hubContext.Clients
                .Group(message.SenderEmail)
                .SendAsync("NewNotification", new
                {
                    title = senderNotification.Title,
                    detail = senderNotification.Detail,
                    imageUrl = senderNotification.ImageUrl,
                    createdAt = senderNotification.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                });
            var categoryName = await GetCategoryNameAsync(message.CategoryId);
            using (BeginMessageScope(message.SenderEmail, message.ReceiverEmail, categoryName, LogContextValues.MessageStatusRead))
            {
                _logger.LogInformation(MessageLogMessages.MessageRead);
            }
        }

        public async Task<List<SelectListItem>> GetCategorySelectListAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return categories.Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.CategoryId.ToString()
            }).ToList();
        }

        public async Task<string?> GetCategoryNameAsync(int categoryId)
        {
            return await _context.Categories
                .Where(category => category.CategoryId == categoryId)
                .Select(category => category.CategoryName)
                .FirstOrDefaultAsync();
        }

        private IDisposable BeginMessageScope(string senderEmail, string receiverEmail, string? categoryName, string messageStatus)
        {
            var scope = new Dictionary<string, object?>
            {
                ["OperationType"] = LogContextValues.OperationMessage,
                ["SenderEmail"] = senderEmail,
                ["ReceiverEmail"] = receiverEmail,
                ["Category"] = categoryName ?? LogContextValues.CategoryFallback,
                ["MessageStatus"] = messageStatus
            };

            return _logger.BeginScope(scope);
        }

        private static string GetMessageStatus(Message message)
        {
            if (message.IsDeleted)
            {
                return LogContextValues.MessageStatusTrash;
            }

            if (message.IsDraft)
            {
                return LogContextValues.MessageStatusDraft;
            }

            return message.IsRead ? LogContextValues.MessageStatusRead : LogContextValues.MessageStatusUnread;
        }
        private async Task<Notification> CreateNotificationAsync(
                    string title,
                    string detail,
                    string? recipientEmail,
                    string? recipientRole,
                    string? imageUrl = null)
        {
            var notification = new Notification
            {
                Title = title,
                Detail = detail,
                RecipientEmail = recipientEmail,
                RecipientRole = recipientRole,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }
    }
}
