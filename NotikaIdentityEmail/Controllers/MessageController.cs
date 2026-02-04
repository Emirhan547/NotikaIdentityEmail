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

        // 📥 Inbox
        public async Task<IActionResult> Inbox(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("Inbox access failed - user not found in db. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            Log.Information("Inbox opened. User: {UserEmail}, Query: {Query}", user.Email, query);

            var values = from m in _context.Messages
                         join u in _context.Users
                         on m.SenderEmail equals u.Email into userGroup
                         from sender in userGroup.DefaultIfEmpty()

                         join c in _context.Categories
                         on m.CategoryId equals c.CategoryId into categoryGroup
                         from category in categoryGroup.DefaultIfEmpty()

                         where m.ReceiverEmail == user.Email && !m.IsDeleted && !m.IsDraft
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

            Log.Information("Inbox listed. User: {UserEmail}, Count: {Count}, Query: {Query}", user.Email, list.Count, query);

            return View(list);
        }

        // 📤 Sendbox
        public async Task<IActionResult> Sendbox(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("Sendbox access failed - user not found in db. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            Log.Information("Sendbox opened. User: {UserEmail}, Query: {Query}", user.Email, query);

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

            Log.Information("Sendbox listed. User: {UserEmail}, Count: {Count}, Query: {Query}", user.Email, list.Count, query);

            return View(list);
        }

        // 👁 Message Detail (okundu işaretleme)
        public async Task<IActionResult> MessageDetail(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("MessageDetail access failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            var value = await _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && !x.IsDeleted);
            if (value == null)
            {
                Log.Warning("MessageDetail not found. User: {UserEmail}, MessageId: {MessageId}", user.Email, id);
                return NotFound();
            }

            // Yetki kontrolü (message sadece sender/receiver tarafından görülmeli)
            if (value.SenderEmail != user.Email && value.ReceiverEmail != user.Email)
            {
                Log.Warning("MessageDetail forbidden. User: {UserEmail}, MessageId: {MessageId}, Sender: {Sender}, Receiver: {Receiver}",
                    user.Email, id, value.SenderEmail, value.ReceiverEmail);
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

                Log.Information(
                    "Message read notification sent. MessageId: {MessageId}, Sender: {Sender}, Reader: {Reader}",
                    value.MessageId,
                    value.SenderEmail,
                    user.Email
                );
            }


            Log.Information("MessageDetail opened. User: {UserEmail}, MessageId: {MessageId}, IsRead: {IsRead}", user.Email, value.MessageId, value.IsRead);

            return View(value);
        }

        // ✉️ Compose GET
        [HttpGet]
        public IActionResult ComposeMessage()
        {
            LoadCategories();
            Log.Information("ComposeMessage GET opened.");
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
                Log.Warning("ComposeMessage POST failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            var isDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase);

            Log.Information(
                LogMessages.MessageComposeStarted,
                user.Email,
                model.ReceiverEmail,
                isDraft,
                model.CategoryId
            );

            // Draft değilse receiver doğrula
            if (!isDraft)
            {
                var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail);
                if (receiver == null)
                {
                    ModelState.AddModelError(nameof(model.ReceiverEmail), "Alıcı email adresi sistemde bulunamadı.");

                    Log.Warning("ComposeMessage validation failed - receiver not found. Sender: {Sender}, Receiver: {Receiver}, CategoryId: {CategoryId}",
                        user.Email, model.ReceiverEmail, model.CategoryId);
                }
            }

            if (!ModelState.IsValid)
            {
                LoadCategories();
                Log.Warning("ComposeMessage ModelState invalid. Sender: {Sender}, Receiver: {Receiver}, IsDraft: {IsDraft}, CategoryId: {CategoryId}",
                    user.Email, model.ReceiverEmail, isDraft, model.CategoryId);
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

            Log.Information(LogMessages.MessageComposeSucceeded, message.MessageId);
            // 🔔 Draft değilse alıcıya realtime bildirim
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

                Log.Information("SignalR NewMessage sent. Sender: {Sender}, Receiver: {Receiver}, MessageId: {MessageId}",
                    message.SenderEmail, message.ReceiverEmail, message.MessageId);
            }


            return RedirectToAction(message.IsDraft ? "Draft" : "Sendbox");
        }

        // 📂 Category filter
        public async Task<IActionResult> GetMessageListByCategory(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("GetMessageListByCategory failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            Log.Information("GetMessageListByCategory opened. User: {UserEmail}, CategoryId: {CategoryId}", user.Email, id);

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

            Log.Information("GetMessageListByCategory listed. User: {UserEmail}, CategoryId: {CategoryId}, Count: {Count}", user.Email, id, values.Count);

            return View(values);
        }

        // 📝 Draft
        public async Task<IActionResult> Draft(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("Draft access failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            Log.Information("Draft opened. User: {UserEmail}, Query: {Query}", user.Email, query);

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

            Log.Information("Draft listed. User: {UserEmail}, Count: {Count}, Query: {Query}", user.Email, list.Count, query);

            return View(list);
        }

        // 🗑 Trash
        public async Task<IActionResult> Trash(string? query)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("Trash access failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            Log.Information("Trash opened. User: {UserEmail}, Query: {Query}", user.Email, query);

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

            Log.Information("Trash listed. User: {UserEmail}, Count: {Count}, Query: {Query}", user.Email, list.Count, query);

            return View(list);
        }

        // ↩️ Reply
        public async Task<IActionResult> Reply(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("Reply failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id && x.ReceiverEmail == user.Email && !x.IsDeleted);

            if (message == null)
            {
                Log.Warning("Reply not found or forbidden. User: {UserEmail}, MessageId: {MessageId}", user.Email, id);
                return NotFound();
            }

            LoadCategories();

            var model = new ComposeMessageViewModel
            {
                ReceiverEmail = message.SenderEmail,
                Subject = $"Re: {message.Subject}"
            };

            Log.Information("Reply prepared. User: {UserEmail}, MessageId: {MessageId}, Receiver: {ReceiverEmail}", user.Email, id, model.ReceiverEmail);

            return View("ComposeMessage", model);
        }

        // ➡️ Forward
        public async Task<IActionResult> Forward(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("Forward failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id &&
                (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email) &&
                !x.IsDeleted);

            if (message == null)
            {
                Log.Warning("Forward not found or forbidden. User: {UserEmail}, MessageId: {MessageId}", user.Email, id);
                return NotFound();
            }

            LoadCategories();

            var model = new ComposeMessageViewModel
            {
                Subject = $"Fwd: {message.Subject}",
                MessageDetail = message.MessageDetail
            };

            Log.Information("Forward prepared. User: {UserEmail}, MessageId: {MessageId}", user.Email, id);

            return View("ComposeMessage", model);
        }

        // 🗑 Move to Trash (soft delete)
        public async Task<IActionResult> MoveToTrash(int id, string returnAction = "Inbox")
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);

            if (user == null)
            {
                Log.Warning("MoveToTrash failed - user not found. IdentityName: {IdentityName}", User.Identity?.Name);
                return Unauthorized();
            }

            var message = await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id &&
                !x.IsDeleted &&
                (x.SenderEmail == user.Email || x.ReceiverEmail == user.Email));

            if (message == null)
            {
                Log.Warning("MoveToTrash not found or forbidden. User: {UserEmail}, MessageId: {MessageId}", user.Email, id);
                return NotFound();
            }

            message.IsDeleted = true;
            message.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            Log.Information(
                LogMessages.MessageMoveToTrash,
                message.MessageId,
                user.Email
            );

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
    }
}
