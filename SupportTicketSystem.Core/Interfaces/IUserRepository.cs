using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<IEnumerable<User>> GetAvailableAgentsAsync();
        Task<User?> GetUserWithSkillsAsync(int userId);
        Task<bool> EmailExistsAsync(string email);
    }
}
