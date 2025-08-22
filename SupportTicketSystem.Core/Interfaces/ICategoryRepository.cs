using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface ICategoryRepository : IBaseRepository<Category>
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentCategoryId);
        Task<Category?> GetCategoryWithSubCategoriesAsync(int categoryId);
        Task<IEnumerable<Category>> GetCategoryHierarchyAsync();
        Task<bool> HasSubCategoriesAsync(int categoryId);
        Task<bool> HasTicketsAsync(int categoryId);
    }
}
