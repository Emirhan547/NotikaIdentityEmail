
using NotikaIdentityEmail.Models.Elastics;

namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // 🔹 Genel sayılar
        public int UserCount { get; set; }
        public int MessageCount { get; set; }
        public int UnreadMessageCount { get; set; }
        public int DraftCount { get; set; }
        public int TrashCount { get; set; }
        public int NotificationCount { get; set; }
        public int CommentCount { get; set; }
        public int CategoryCount { get; set; }

        // 🔹 DB'den gelen listeler
        public List<RecentMessageViewModel> RecentMessages { get; set; } = new();
        public List<CategoryStatViewModel> CategoryStats { get; set; } = new();

        // 🔥 Elasticsearch / Observability
        public int ErrorCountLast24h { get; set; }
        public List<ElasticLogItemDto> LatestElasticLogs { get; set; } = new();
        public List<string> WeeklyMessageLabels { get; set; } = new();
        public List<int> WeeklyMessageCounts { get; set; } = new();
        public List<int> WeeklyUnreadMessageCounts { get; set; } = new();
        // 🔧 Sistem / Konfig
        public string? ElasticsearchUrl { get; set; }
        public string? KibanaUrl { get; set; }

        public bool ElasticsearchEnabled { get; set; }
        public bool KibanaEnabled { get; set; }
    }
}