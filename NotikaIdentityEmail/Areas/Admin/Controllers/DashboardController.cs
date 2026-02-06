using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Services;
using NotikaIdentityEmail.Services.DashboardServices;

namespace NotikaIdentityEmail.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }
     

        [HttpGet]
        public async Task<IActionResult> GetErrorCount()
        {
            var count = await _dashboardService.GetErrorCountLast24hAsync();
            return Json(new { count });
        }
        public async Task<IActionResult> Index()
        {
            var model = await _dashboardService.BuildDashboardAsync();
            return View(model);
        }
    }
}