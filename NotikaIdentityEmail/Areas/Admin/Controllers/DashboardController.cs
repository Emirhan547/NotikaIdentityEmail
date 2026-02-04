using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Services;

namespace NotikaIdentityEmail.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ElasticLogService _elasticLogService;

        public DashboardController(
            EmailContext context,
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            ElasticLogService elasticLogService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _elasticLogService = elasticLogService;
        }
        // Areas/Admin/Controllers/DashboardController.cs'e ekle

        [HttpGet]
        public async Task<IActionResult> GetErrorCount()
        {
            var count = await _elasticLogService.GetErrorCountLast24hAsync();
            return Json(new { count });
        }
        public async Task<IActionResult> Index()
        {
            // 🔹 DB tarafı (senin mevcut yapı)
            var recentMessages = await _context.Messages
                .Include(x => x.Category)
                .OrderByDescending(x => x.SendDate)
                .Take(6)
                .Select(x => new RecentMessageViewModel
                {
                    SenderEmail = x.SenderEmail,
                    Subject = x.Subject,
                    CategoryName = x.Category.CategoryName,
                    SendDate = x.SendDate,
                    IsRead = x.IsRead
                })
                .ToListAsync();

            var categoryStats = await _context.Messages
                .Include(x => x.Category)
                .GroupBy(x => x.Category.CategoryName)
                .Select(group => new CategoryStatViewModel
                {
                    CategoryName = group.Key,
                    MessageCount = group.Count()
                })
                .OrderByDescending(x => x.MessageCount)
                .Take(6)
                .ToListAsync();

            // 🔹 Elasticsearch tarafı
            var latestLogs = await _elasticLogService.GetLatestAsync(10);
            var errorCountLast24h = await _elasticLogService.GetErrorCountLast24hAsync();

            var model = new DashboardViewModel
            {
                CategoryCount = await _context.Categories.CountAsync(),
                MessageCount = await _context.Messages.CountAsync(),
                UnreadMessageCount = await _context.Messages.CountAsync(x => !x.IsRead && !x.IsDeleted),
                DraftCount = await _context.Messages.CountAsync(x => x.IsDraft),
                TrashCount = await _context.Messages.CountAsync(x => x.IsDeleted),
                NotificationCount = await _context.Notifications.CountAsync(),
                CommentCount = await _context.Comments.CountAsync(),
                UserCount = await _userManager.Users.CountAsync(),

                RecentMessages = recentMessages,
                CategoryStats = categoryStats,

                // 🔥 Elastic bilgiler
                LatestElasticLogs = latestLogs,
                ErrorCountLast24h = errorCountLast24h,

                ElasticsearchUrl = _configuration["Elastic:BaseUrl"],
                KibanaUrl = _configuration["Kibana:Url"],
                SerilogMinimumLevel = _configuration["Serilog:MinimumLevel:Default"],
                ElasticsearchEnabled = !string.IsNullOrWhiteSpace(_configuration["Elastic:BaseUrl"]),
                KibanaEnabled = !string.IsNullOrWhiteSpace(_configuration["Kibana:Url"])
            };

            return View(model);
        }
    }
}
