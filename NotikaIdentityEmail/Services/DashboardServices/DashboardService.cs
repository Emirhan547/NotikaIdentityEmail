using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using System.Globalization;

namespace NotikaIdentityEmail.Services.DashboardServices
{
    public class DashboardService:IDashboardService
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ElasticLogService _elasticLogService;

        public DashboardService(ElasticLogService elasticLogService, IConfiguration configuration, UserManager<AppUser> userManager, EmailContext context)
        {
            _elasticLogService = elasticLogService;
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
        }
        public async Task<int> GetErrorCountLast24hAsync()
        {
            return await _elasticLogService.GetErrorCountLast24hAsync();
        }

        public async Task<DashboardViewModel> BuildDashboardAsync()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);
            var culture = new CultureInfo("tr-TR");

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

            var weeklyRawData = await _context.Messages
                .Where(x => x.SendDate.Date >= weekStart && x.SendDate.Date <= today)
                .GroupBy(x => x.SendDate.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    Total = group.Count(),
                    Unread = group.Count(m => !m.IsRead)
                })
                .ToListAsync();

            var weeklyMessageLabels = new List<string>();
            var weeklyMessageCounts = new List<int>();
            var weeklyUnreadMessageCounts = new List<int>();

            for (var date = weekStart; date <= today; date = date.AddDays(1))
            {
                var row = weeklyRawData.FirstOrDefault(x => x.Date == date);
                weeklyMessageLabels.Add(date.ToString("ddd", culture));
                weeklyMessageCounts.Add(row?.Total ?? 0);
                weeklyUnreadMessageCounts.Add(row?.Unread ?? 0);
            }

            var latestLogs = await _elasticLogService.GetLatestAsync(10);
            var errorCountLast24h = await _elasticLogService.GetErrorCountLast24hAsync();

            return new DashboardViewModel
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
                WeeklyMessageLabels = weeklyMessageLabels,
                WeeklyMessageCounts = weeklyMessageCounts,
                WeeklyUnreadMessageCounts = weeklyUnreadMessageCounts,
                LatestElasticLogs = latestLogs,
                ErrorCountLast24h = errorCountLast24h,
                ElasticsearchUrl = _configuration["Elastic:BaseUrl"],
                KibanaUrl = _configuration["Kibana:Url"],
                KibanaEnabled = !string.IsNullOrWhiteSpace(_configuration["Kibana:Url"])
            };
        }
    }
}
