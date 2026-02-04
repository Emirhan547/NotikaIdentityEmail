using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotikaIdentityEmail.Services;

namespace NotikaIdentityEmail.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly ElasticLogService _elastic;

        public LogsController(ElasticLogService elastic)
        {
            _elastic = elastic;
        }

        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var log = await _elastic.GetByIdAsync(id);
            if (log == null)
                return NotFound();

            return View(log);
        }
        public async Task<IActionResult> Index(string? level, string? q)
        {
            var logs = await _elastic.GetLatestAsync(100);

            if (!string.IsNullOrWhiteSpace(level))
                logs = logs.Where(x => x.Level == level).ToList();

            if (!string.IsNullOrWhiteSpace(q))
                logs = logs.Where(x =>
                    (x.MessageTemplate ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.RenderedMessage ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserEmail ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            return View(logs);
        }

    }
}
