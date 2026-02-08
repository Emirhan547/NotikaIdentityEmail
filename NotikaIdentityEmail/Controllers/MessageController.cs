using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;

using NotikaIdentityEmail.Services.MessageServices;

namespace NotikaIdentityEmail.Controllers
{
    [Authorize(Roles = "User")]
    public class MessageController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMessageService _messageService;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            UserManager<AppUser> userManager,
            IMessageService messageService,
            ILogger<MessageController> logger)
        {
            _userManager = userManager;
            _messageService = messageService;
            _logger = logger;
        }

        // 📥 Inbox
        public async Task<IActionResult> Inbox(string? query, int page = 1)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var list = await _messageService.GetInboxAsync(user.Email!, query);
            var orderedList = list
                 .OrderByDescending(x => x.SendDate)
                 .ToList();

            const int pageSize = 10;
            var totalCount = orderedList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var currentPage = page < 1 ? 1 : page;
            if (totalPages > 0 && currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            var pagedList = orderedList
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.Query = query;

            return View(pagedList);
        }

        // 📤 Sendbox
        public async Task<IActionResult> Sendbox(string? query, int page = 1)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var list = await _messageService.GetSendboxAsync(user.Email!, query);
            var orderedList = list
                 .OrderByDescending(x => x.SendDate)
                 .ToList();

            const int pageSize = 10;
            var totalCount = orderedList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var currentPage = page < 1 ? 1 : page;
            if (totalPages > 0 && currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            var pagedList = orderedList
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.Query = query;

            return View(pagedList);
        }

        // 👁 Message Detail
        public async Task<IActionResult> MessageDetail(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var message = await _messageService.GetMessageForUserAsync(id, user.Email!);
            if (message == null)
            {
                using (_logger.BeginScope(BuildOperationScope(LogContextValues.OperationMessage, user.Email)))
                {
                    _logger.LogWarning(LogMessages.MessageNotFound);
                }
                return NotFound();
            }

            await _messageService.MarkAsReadAsync(message, user.Email!);
            return View(message);
        }

        // ✉️ Compose GET
        [HttpGet]
        public async Task<IActionResult> ComposeMessage()
        {
            await LoadCategoriesAsync();
            return View(new ComposeMessageViewModel());
        }

        // ✉️ Compose POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComposeMessage(ComposeMessageViewModel model, string action)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var isDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase);

            if (!isDraft)
            {
                var receiverExists = await _messageService.ReceiverExistsAsync(model.ReceiverEmail);
                if (!receiverExists)
                {
                    ModelState.AddModelError(nameof(model.ReceiverEmail), "Alıcı email adresi sistemde bulunamadı.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            var message = await _messageService.CreateMessageAsync(
                user.Email!,
                model,
                isDraft
            );

            return RedirectToAction(message.IsDraft ? "Draft" : "Sendbox");
        }

        // 📂 Category Filter
        public async Task<IActionResult> GetMessageListByCategory(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var list = await _messageService.GetByCategoryAsync(user.Email!, id);
            return View(list);
        }

        // 📝 Draft
        public async Task<IActionResult> Draft(string? query)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var list = await _messageService.GetDraftsAsync(user.Email!, query);
            return View(list);
        }

        // 🗑 Trash
        public async Task<IActionResult> Trash(string? query)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var list = await _messageService.GetTrashAsync(user.Email!, query);
            return View(list);
        }

        // ↩️ Reply
        public async Task<IActionResult> Reply(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var model = await _messageService.BuildReplyModelAsync(id, user.Email!);
            if (model == null) return NotFound();

            await LoadCategoriesAsync();
            return View("ComposeMessage", model);
        }

        // ➡️ Forward
        public async Task<IActionResult> Forward(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var model = await _messageService.BuildForwardModelAsync(id, user.Email!);
            if (model == null) return NotFound();

            await LoadCategoriesAsync();
            return View("ComposeMessage", model);
        }

        // 🗑 Move To Trash
        public async Task<IActionResult> MoveToTrash(int id, string returnAction = "Inbox")
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var message = await _messageService.MoveToTrashAsync(id, user.Email!);
            if (message == null) return NotFound();

            return RedirectToAction(returnAction);
        }

        // 🔐 Helpers
        private async Task<AppUser?> GetCurrentUserAsync()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user != null) return user;

            using (_logger.BeginScope(BuildOperationScope(LogContextValues.OperationMessage)))
            {
                _logger.LogWarning(LogMessages.UserNotFound);
            }

            return null;
        }

        private async Task LoadCategoriesAsync()
        {
            ViewBag.v = await _messageService.GetCategorySelectListAsync();
        }

        private static Dictionary<string, object?> BuildOperationScope(
            string operationType,
            string? userEmail = null)
        {
            var scope = new Dictionary<string, object?>
            {
                ["OperationType"] = operationType
            };

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                scope["UserEmail"] = userEmail;
            }

            return scope;
        }
    }
}
