using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Services.RoleServices;

namespace NotikaIdentityEmail.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }
        public async Task<IActionResult> RoleList()
        {
            var roles = await _roleService.GetRolesAsync();
            return View(roles);
        }
        public IActionResult CreateRole()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            await _roleService.CreateRoleAsync(model.RoleName);
            return RedirectToAction("RoleList");
        }
        public async Task<IActionResult> DeleteRole(string id)
        {
            var deleted = await _roleService.DeleteRoleAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return RedirectToAction("RoleList");
        }
        [HttpGet]
        public async Task<IActionResult> UpdateRole(string id)
        {
            var model = await _roleService.GetRoleForUpdateAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateRole(UpdateRoleViewModel model)
        {
            var updated = await _roleService.UpdateRoleAsync(model);
            if (!updated)
            {
                return NotFound();
            }
            return RedirectToAction("RoleList");
        }
        public async Task<IActionResult> UserList()
        {
            var users = await _roleService.GetUsersAsync();
            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> AssignRole(string id)
        {
            var user = await _roleService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = await _roleService.GetRoleAssignmentsForUserAsync(user);
            TempData["userId"] = user.Id;
            return View(roles);
        }
        [HttpPost]
        public async Task<IActionResult> AssignRole(List<RoleAssignViewModel> model)
        {
            var userId = TempData["userId"]?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest();

            }
            await _roleService.AssignRolesAsync(userId, model);
            return RedirectToAction("RoleList");
        }
    }
}