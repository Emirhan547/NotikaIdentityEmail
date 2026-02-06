using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.CategoryServices
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        CategoryFormViewModel CreateDefaultModel();
        Task CreateAsync(CategoryFormViewModel model);
        Task<CategoryFormViewModel?> GetEditModelAsync(int id);
        Task<bool> UpdateAsync(CategoryFormViewModel model);
        Task<bool> DeleteAsync(int id);
    }
}
