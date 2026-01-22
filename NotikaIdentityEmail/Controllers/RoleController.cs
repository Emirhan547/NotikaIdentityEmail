using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Controllers
{
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<IActionResult> RoleList()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }
        public IActionResult CreateRole()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            await _roleManager.CreateAsync(new IdentityRole
            {
                Name = model.RoleName,
            });
            return RedirectToAction("RoleList");
        }
        public async Task<IActionResult> DeleteRole(string id)
        {
            var values = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == id);
            await _roleManager.DeleteAsync(values);
            return RedirectToAction("RoleList");
        }
        [HttpGet]
        public async Task<IActionResult> UpdateRole(string id)
        {
            var value = await _roleManager.Roles.FirstOrDefaultAsync(y => y.Id == id);
            UpdateRoleViewModel updateRoleViewModel = new UpdateRoleViewModel()
            {
                RoleId = value.Id,
                RoleName = value.Name
            };
            return View(updateRoleViewModel);

        }
        [HttpPost]
        public async Task<IActionResult> UpdateRole(UpdateRoleViewModel model)
        {
            var values = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == model.RoleId);
            values.Name = model.RoleName;
            await _roleManager.UpdateAsync(values);
            return RedirectToAction("RoleList");
        }
        public async Task<IActionResult> UserList()
        {
            var values = await _userManager.Users.ToListAsync();
            return View(values);
        }
        [HttpGet]
        public async Task<IActionResult> AssignRole(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            TempData["userId"] = user.Id;
            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            List<RoleAssignViewModel> roleAssignViewModels = new List<RoleAssignViewModel>();
            foreach (var item in roles)
            {
                RoleAssignViewModel model = new RoleAssignViewModel();
                model.RoleId = item.Id;
                model.RoleName = item.Name;
                model.RoleExist = userRoles.Contains(item.Name);
                roleAssignViewModels.Add(model);
            }
            return View(roleAssignViewModels);
        }
        [HttpPost]
        public async Task<IActionResult> AssignRole(List<RoleAssignViewModel> model)
        {
            var userId = TempData["userId"].ToString(  );
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            foreach (var item in model)
            {
                if (item.RoleExist)
                {
                    await _userManager.AddToRoleAsync(user, item.RoleName);
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, item.RoleName);
                }

            }
            return RedirectToAction("RoleList");
        }
    }
}