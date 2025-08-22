using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface ITicketRepository : IBaseRepository<Ticket>
    {
        Task<IEnumerable<Ticket>> GetTicketsByStatusAsync(TicketStatus status);
        Task<IEnumerable<Ticket>> GetTicketsByCustomerAsync(int customerId);
        Task<IEnumerable<Ticket>> GetTicketsByAgentAsync(int agentId);
        Task<IEnumerable<Ticket>> GetTicketsByCategoryAsync(int categoryId);
        Task<Ticket?> GetTicketWithDetailsAsync(int ticketId);
        Task<string> GenerateTicketNumberAsync();
        Task<IEnumerable<Ticket>> GetOverdueTicketsAsync();
        Task<IEnumerable<Ticket>> SearchTicketsAsync(string searchTerm);
    }
}
