using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => u.Role == role && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAvailableAgentsAsync()
        {
            return await _dbSet
                .Where(u => u.Role == UserRole.Agent && u.IsActive)
                .Include(u => u.AgentSkills)
                    .ThenInclude(as_ => as_.Skill)
                .Include(u => u.AssignedTickets.Where(t => t.Status == TicketStatus.InProgress))
                .OrderBy(u => u.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress))
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<User?> GetUserWithSkillsAsync(int userId)
        {
            return await _dbSet
                .Where(u => u.Id == userId)
                .Include(u => u.AgentSkills)
                    .ThenInclude(as_ => as_.Skill)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
    }
}
