using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Services.RoleServices
{
    public class RoleService:IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<List<IdentityRole>> GetRolesAsync() => await _roleManager.Roles.ToListAsync();

        public async Task CreateRoleAsync(string roleName)
        {
            await _roleManager.CreateAsync(new IdentityRole { Name = roleName });
        }

        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == roleId);
            if (role == null)
            {
                return false;
            }

            await _roleManager.DeleteAsync(role);
            return true;
        }

        public async Task<UpdateRoleViewModel?> GetRoleForUpdateAsync(string roleId)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(y => y.Id == roleId);
            if (role == null)
            {
                return null;
            }

            return new UpdateRoleViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name
            };
        }

        public async Task<bool> UpdateRoleAsync(UpdateRoleViewModel model)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == model.RoleId);
            if (role == null)
            {
                return false;
            }

            role.Name = model.RoleName;
            await _roleManager.UpdateAsync(role);
            return true;
        }

        public async Task<List<AppUser>> GetUsersAsync() => await _userManager.Users.ToListAsync();

        public async Task<AppUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<List<RoleAssignViewModel>> GetRoleAssignmentsForUserAsync(AppUser user)
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            return roles.Select(item => new RoleAssignViewModel
            {
                RoleId = item.Id,
                RoleName = item.Name,
                RoleExist = userRoles.Contains(item.Name)
            }).ToList();
        }

        public async Task AssignRolesAsync(string userId, List<RoleAssignViewModel> model)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return;
            }

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
        }
    }
}
