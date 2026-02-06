using Microsoft.AspNetCore.Identity;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Services.RoleServices
{
    public interface IRoleService
    {
        Task<List<IdentityRole>> GetRolesAsync();
        Task CreateRoleAsync(string roleName);
        Task<bool> DeleteRoleAsync(string roleId);
        Task<UpdateRoleViewModel?> GetRoleForUpdateAsync(string roleId);
        Task<bool> UpdateRoleAsync(UpdateRoleViewModel model);
        Task<List<AppUser>> GetUsersAsync();
        Task<AppUser?> GetUserByIdAsync(string userId);
        Task<List<RoleAssignViewModel>> GetRoleAssignmentsForUserAsync(AppUser user);
        Task AssignRolesAsync(string userId, List<RoleAssignViewModel> model);
    }
}
