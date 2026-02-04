namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int CategoryCount { get; set; }
        public int MessageCount { get; set; }
        public int UnreadMessageCount { get; set; }
        public int DraftCount { get; set; }
        public int TrashCount { get; set; }
        public int NotificationCount { get; set; }
        public int CommentCount { get; set; }
        public int UserCount { get; set; }

        public string? ElasticsearchUrl { get; set; }
        public string? KibanaUrl { get; set; }
        public string? SerilogMinimumLevel { get; set; }
        public bool ElasticsearchEnabled { get; set; }
        public bool KibanaEnabled { get; set; }

        public List<RecentMessageViewModel> RecentMessages { get; set; } = new();
        public List<CategoryStatViewModel> CategoryStats { get; set; } = new();
    }
}

