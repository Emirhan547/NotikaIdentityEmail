using NotikaIdentityEmail.Areas.Admin.Models;

namespace NotikaIdentityEmail.Services.DashboardServices
{
    public interface IDashboardService
    {
        Task<int> GetErrorCountLast24hAsync();
        Task<DashboardViewModel> BuildDashboardAsync();
    }
}

