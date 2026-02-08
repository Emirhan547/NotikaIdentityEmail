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
            var recentComments = await _context.Comments
                .Include(x => x.AppUser)
                .OrderByDescending(x => x.CommentDate)
                .Take(6)
                .Select(comment => new AdminCommentViewModel
                {
                    CommentId = comment.CommentId,
                    Username = comment.AppUser.UserName ?? string.Empty,
                    FullName = $"{comment.AppUser.Name} {comment.AppUser.Surname}".Trim(),
                    CommentDetail = comment.CommentDetail,
                    ToxicityLabel = comment.ToxicityLabel,
                    ToxicityScore = comment.ToxicityScore,
                    IsToxic = comment.IsToxic,
                    Status = comment.CommentStatus,
                    CommentDate = comment.CommentDate
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
                .Take(10)
                .ToListAsync();

          

            var latestLogs = await _elasticLogService.GetLatestAsync(10);
            var errorCountLast24h = await _elasticLogService.GetErrorCountLast24hAsync();

            return new DashboardViewModel
            {
                CategoryCount = await _context.Categories.CountAsync(),
                MessageCount = await _context.Messages.CountAsync(),
                UnreadMessageCount = await _context.Messages.CountAsync(x => !x.IsRead && !x.IsDeleted),
                DraftCount = await _context.Messages.CountAsync(x => x.IsDraft),
                TrashCount = await _context.Messages.CountAsync(x => x.IsDeleted),
                NotificationCount = await _context.Notifications.CountAsync(x => x.RecipientRole == "Admin"),
                CommentCount = await _context.Comments.CountAsync(),
                ToxicCommentCount = await _context.Comments.CountAsync(x => x.IsToxic),
                UserCount = await _userManager.Users.CountAsync(),
                RecentMessages = recentMessages,
                RecentComments = recentComments,
                CategoryStats = categoryStats,
               
                LatestElasticLogs = latestLogs,
                ErrorCountLast24h = errorCountLast24h,
                ElasticsearchUrl = _configuration["Elastic:BaseUrl"],
                KibanaUrl = _configuration["Kibana:Url"],
                KibanaEnabled = !string.IsNullOrWhiteSpace(_configuration["Kibana:Url"])
            };
        }
    }
}
