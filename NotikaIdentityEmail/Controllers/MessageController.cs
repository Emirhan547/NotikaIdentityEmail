using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Services;
using Microsoft.AspNetCore.SignalR;
using NotikaIdentityEmail.Hubs;

using Serilog;
using ILogger = Serilog.ILogger;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize(Roles = "User")]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHtmlSanitizerService _htmlSanitizer;
        private readonly IHubContext<NotificationHub> _hubContext;
        public MessageController(
            EmailContext context,
            UserManager<AppUser> userManager,
            IHtmlSanitizerService htmlSanitizer,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _htmlSanitizer = htmlSanitizer;
            _hubContext = hubContext;
        }

        // MessageController.cs - Inbox metodunu değiştir

        public async Task<IActionResult> Inbox(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                     .Warning(LogMessages.UserNotFound);
                return Unauthorized();
            }

          

            // ✅ DÜZELTME: N+1 problemi çözüldü - tek query
            var values = from m in _context.Messages
                         join sender in _context.Users on m.SenderEmail equals sender.Email
                         join category in _context.Categories on m.CategoryId equals category.CategoryId
                         where m.ReceiverEmail == user.Email && !m.IsDeleted && !m.IsDraft
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

            var list = await values.ToListAsync();

          

            return View(list);
        }



        // 📤 Sendbox
        public async Task<IActionResult> Sendbox(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                    .Warning(LogMessages.UserNotFound);
                return Unauthorized();
            }



            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.ReceiverEmail equals u.Email into userGroup
                         from receiver in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.SenderEmail == user.Email && !m.IsDeleted && !m.IsDraft
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

            var list = await values.ToListAsync();


            return View(list);
        }

        // 👁 Message Detail (okundu işaretleme)
        public async Task<IActionResult> MessageDetail(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.UserNotFound);
                return Unauthorized();
            }

            var value = await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && !x.IsDeleted);
            if (value == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.MessageNotFound);
                return NotFound();
            }

            // Yetki kontrolü (message sadece sender/receiver tarafından görülmeli)
            if (value.SenderEmail != user.Email && value.ReceiverEmail != user.Email)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                     .Warning(LogMessages.AccessForbidden);
                return Forbid();
            }

            if (!value.IsRead && value.ReceiverEmail == user.Email)
            {
                value.IsRead = true;
                await _context.SaveChangesAsync();

                // 🔔 Gönderene "okundu" bildirimi
                await _hubContext.Clients
                    .Group(value.SenderEmail)
                    .SendAsync("MessageRead", new
                    {
                        messageId = value.MessageId,
                        readerEmail = user.Email,
                        subject = value.Subject
                    });

                var categoryName = await GetCategoryNameAsync(value.CategoryId);
                var logger = CreateMessageLogger(value.SenderEmail, value.ReceiverEmail, categoryName, LogContextValues.MessageStatusRead);
                logger.Information(MessageLogMessages.MessageRead);
            }



            return View(value);
        }

        // ✉️ Compose GET
        [HttpGet]
        public IActionResult ComposeMessage()
        {
            LoadCategories();
         
            return View(new ComposeMessageViewModel());
        }

        // ✉️ Compose POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComposeMessage(ComposeMessageViewModel model, string action)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.UserNotFound); return Unauthorized();
            }

            var isDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase);


            // Draft değilse receiver doğrula
            if (!isDraft)
            {
                var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail);
                if (receiver == null)
                {
                    ModelState.AddModelError(nameof(model.ReceiverEmail), "Alıcı email adresi sistemde bulunamadı.");

                    Log.ForContext("OperationType", LogContextValues.OperationMessage)
                        .Warning(LogMessages.UserNotFound);
                }
            }

            if (!ModelState.IsValid)
            {
                LoadCategories();
                
                return View(model);
            }

            var message = new Message
            {
                SenderEmail = user.Email!,
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
            var logger = CreateMessageLogger(message.SenderEmail, message.ReceiverEmail, categoryName, status);
            logger.Information(message.IsDraft ? MessageLogMessages.MessageDraftSaved : MessageLogMessages.MessageSent);            // 🔔 Draft değilse alıcıya realtime bildirim
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

               
            }


            return RedirectToAction(message.IsDraft ? "Draft" : "Sendbox");
        }

        // 📂 Category filter
        public async Task<IActionResult> GetMessageListByCategory(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.UserNotFound); return Unauthorized();
            }


            var values = (from m in _context.Messages
                          join u in _context.Users
                          on m.SenderEmail equals u.Email into userGroup
                          from sender in userGroup.DefaultIfEmpty()

                          join c in _context.Categories
                          on m.CategoryId equals c.CategoryId into categoryGroup
                          from category in categoryGroup.DefaultIfEmpty()

                          where m.ReceiverEmail == user.Email && m.CategoryId == id && !m.IsDeleted && !m.IsDraft
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
                          }).ToList();


            return View(values);
        }

        // 📝 Draft
        public async Task<IActionResult> Draft(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                   .Warning(LogMessages.UserNotFound); return Unauthorized();
            }


            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.ReceiverEmail equals u.Email into userGroup
                         from receiver in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.SenderEmail == user.Email && m.IsDraft && !m.IsDeleted
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

            var list = await values.ToListAsync();


            return View(list);
        }

        // 🗑 Trash
        public async Task<IActionResult> Trash(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.UserNotFound); return Unauthorized();
            }


            var values = from m in _context.Messages
                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.IsDeleted && (m.SenderEmail == user.Email || m.ReceiverEmail == user.Email)
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

            var list = await values.ToListAsync();


            return View(list);
        }

        // ↩️ Reply
        public async Task<IActionResult> Reply(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                    .Warning(LogMessages.UserNotFound);
                return Unauthorized();
            }

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id && x.ReceiverEmail == user.Email && !x.IsDeleted);

            if (message == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                   .Warning(LogMessages.MessageNotFound);
                return NotFound();
            }

            LoadCategories();

            var model = new ComposeMessageViewModel
            {
                ReceiverEmail = message.SenderEmail,
                Subject = $"Re: {message.Subject}"
            };


            return View("ComposeMessage", model);
        }

        // ➡️ Forward
        public async Task<IActionResult> Forward(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.UserNotFound); return Unauthorized();
            }

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id &&
                (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email) &&
                !x.IsDeleted);

            if (message == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.MessageNotFound); return NotFound();
            }

            LoadCategories();

            var model = new ComposeMessageViewModel
            {
                Subject = $"Fwd: {message.Subject}",
                MessageDetail = message.MessageDetail
            };


            return View("ComposeMessage", model);
        }

        // 🗑 Move to Trash (soft delete)
        public async Task<IActionResult> MoveToTrash(int id, string returnAction = "Inbox")
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                   .Warning(LogMessages.UserNotFound); return Unauthorized();
            }

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id &&
                !x.IsDeleted &&
                (x.SenderEmail == user.Email || x.ReceiverEmail == user.Email));

            if (message == null)
            {
                Log.ForContext("OperationType", LogContextValues.OperationMessage)
                                    .Warning(LogMessages.MessageNotFound); return NotFound();
            }

            message.IsDeleted = true;
            message.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var categoryName = await GetCategoryNameAsync(message.CategoryId);
            var logger = CreateMessageLogger(message.SenderEmail, message.ReceiverEmail, categoryName, LogContextValues.MessageStatusTrash);
            logger.Information(MessageLogMessages.MessageMovedToTrash);

            return RedirectToAction(returnAction);
        }

        private void LoadCategories()
        {
            var categories = _context.Categories.ToList();
            ViewBag.v = categories.Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.CategoryId.ToString()
            });
        }
        private ILogger CreateMessageLogger(string senderEmail, string receiverEmail, string? categoryName, string messageStatus)
        {
            return Log.ForContext("OperationType", LogContextValues.OperationMessage)
                .ForContext("SenderEmail", senderEmail)
                .ForContext("ReceiverEmail", receiverEmail)
                .ForContext("Category", categoryName ?? LogContextValues.CategoryFallback)
                .ForContext("MessageStatus", messageStatus);
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

        private async Task<string?> GetCategoryNameAsync(int categoryId)
        {
            return await _context.Categories
                .Where(category => category.CategoryId == categoryId)
                .Select(category => category.CategoryName)
                .FirstOrDefaultAsync();
        }
    }
}