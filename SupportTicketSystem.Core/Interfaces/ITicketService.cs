using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface ITicketService
    {
        Task<Ticket> CreateTicketAsync(Ticket ticket, BusinessImpact? businessImpact = null);
        Task<Ticket?> GetTicketAsync(int ticketId);
        Task<IEnumerable<Ticket>> GetTicketsByUserAsync(int userId, UserRole userRole);
        Task<Ticket> UpdateTicketStatusAsync(int ticketId, TicketStatus status, int userId);
        Task<Ticket> AssignTicketAsync(int ticketId, int agentId, int assignedByUserId);
        Task<TicketComment> AddCommentAsync(int ticketId, int userId, string comment, bool isInternal = false, bool useAIAssistance = false);
        Task<IEnumerable<Ticket>> SearchTicketsAsync(string searchTerm);
        Task<Ticket> UpdateTicketPriorityAsync(int ticketId, Priority priority, int userId);
        Task AutoCategorizeTicketAsync(int ticketId);
        Task<User?> SuggestBestAgentAsync(int ticketId);
    }
}
