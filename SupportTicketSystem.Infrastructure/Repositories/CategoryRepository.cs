using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.Infrastructure.Repositories
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
        {
            return await _dbSet
                .Where(c => c.ParentCategoryId == null && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentCategoryId)
        {
            return await _dbSet
                .Where(c => c.ParentCategoryId == parentCategoryId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryWithSubCategoriesAsync(int categoryId)
        {
            return await _dbSet
                .Where(c => c.Id == categoryId)
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Category>> GetCategoryHierarchyAsync()
        {
            return await _dbSet
                .Where(c => c.IsActive)
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> HasSubCategoriesAsync(int categoryId)
        {
            return await _dbSet
                .AnyAsync(c => c.ParentCategoryId == categoryId && c.IsActive);
        }

        public async Task<bool> HasTicketsAsync(int categoryId)
        {
            return await _context.Tickets
                .AnyAsync(t => t.CategoryId == categoryId);
        }
    }
}
