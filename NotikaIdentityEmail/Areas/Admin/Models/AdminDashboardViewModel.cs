using NotikaIdentityEmail.Models.Elastics;

namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int ErrorCountLast24h { get; set; }
        public List<ElasticLogItemDto> LatestLogs { get; set; } = new();
        public List<ElasticLogItemDto> LatestErrors { get; set; } = new();
    }
}
