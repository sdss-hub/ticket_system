using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.Infrastructure.Repositories
{
    public class TicketRepository : BaseRepository<Ticket>, ITicketRepository
    {
        public TicketRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByStatusAsync(TicketStatus status)
        {
            return await _dbSet
                .Where(t => t.Status == status)
                .Include(t => t.Customer)
                .Include(t => t.AssignedAgent)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByCustomerAsync(int customerId)
        {
            return await _dbSet
                .Where(t => t.CustomerId == customerId)
                .Include(t => t.AssignedAgent)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByAgentAsync(int agentId)
        {
            return await _dbSet
                .Where(t => t.AssignedAgentId == agentId)
                .Include(t => t.Customer)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Where(t => t.CategoryId == categoryId)
                .Include(t => t.Customer)
                .Include(t => t.AssignedAgent)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Ticket?> GetTicketWithDetailsAsync(int ticketId)
        {
            return await _dbSet
                .Where(t => t.Id == ticketId)
                .Include(t => t.Customer)
                .Include(t => t.AssignedAgent)
                .Include(t => t.Category)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .Include(t => t.History)
                    .ThenInclude(h => h.User)
                .Include(t => t.TicketTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.AIInsights)
                .FirstOrDefaultAsync();
        }

        public async Task<string> GenerateTicketNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayTicketCount = await _dbSet
                .Where(t => t.TicketNumber.StartsWith(today))
                .CountAsync();
            
            return $"{today}{(todayTicketCount + 1):D4}";
        }

        public async Task<IEnumerable<Ticket>> GetOverdueTicketsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(t => t.DueDate.HasValue && t.DueDate < now && 
                           t.Status != TicketStatus.Resolved && 
                           t.Status != TicketStatus.Closed)
                .Include(t => t.Customer)
                .Include(t => t.AssignedAgent)
                .Include(t => t.Category)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> SearchTicketsAsync(string searchTerm)
        {
            var term = searchTerm.ToLower();
            return await _dbSet
                .Where(t => t.Title.ToLower().Contains(term) || 
                           t.Description.ToLower().Contains(term) ||
                           t.TicketNumber.Contains(term))
                .Include(t => t.Customer)
                .Include(t => t.AssignedAgent)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
