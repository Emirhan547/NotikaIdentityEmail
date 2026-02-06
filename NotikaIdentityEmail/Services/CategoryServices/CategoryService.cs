using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Areas.Admin.Models;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Services.CategoryServices
{
    public class CategoryService:ICategoryService
    {
        private readonly EmailContext _context;

        public CategoryService(EmailContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories.OrderBy(x => x.CategoryName).ToListAsync();
        }

        public CategoryFormViewModel CreateDefaultModel() => new() { CategoryStatus = true };

        public async Task CreateAsync(CategoryFormViewModel model)
        {
            var category = new Category
            {
                CategoryName = model.CategoryName,
                CategoryIconUrl = model.CategoryIconUrl ?? string.Empty,
                CategoryStatus = model.CategoryStatus
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task<CategoryFormViewModel?> GetEditModelAsync(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (category == null)
            {
                return null;
            }

            return new CategoryFormViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                CategoryIconUrl = category.CategoryIconUrl,
                CategoryStatus = category.CategoryStatus
            };
        }

        public async Task<bool> UpdateAsync(CategoryFormViewModel model)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == model.CategoryId);
            if (category == null)
            {
                return false;
            }

            category.CategoryName = model.CategoryName;
            category.CategoryIconUrl = model.CategoryIconUrl ?? string.Empty;
            category.CategoryStatus = model.CategoryStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (category == null)
            {
                return false;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
    
