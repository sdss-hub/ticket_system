using System.Text.Json;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;

namespace SupportTicketSystem.Core.Services
{
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAIService _aiService;

        public TicketService(IUnitOfWork unitOfWork, IAIService aiService)
        {
            _unitOfWork = unitOfWork;
            _aiService = aiService;
        }

        public async Task<Ticket> CreateTicketAsync(Ticket ticket)
        {
            // Generate ticket number
            ticket.TicketNumber = await _unitOfWork.Tickets.GenerateTicketNumberAsync();
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            // Add ticket
            await _unitOfWork.Tickets.AddAsync(ticket);
            
            // Create history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticket.Id,
                UserId = ticket.CustomerId,
                Action = "Created",
                NewValue = ticket.Status.ToString(),
                Details = JsonSerializer.Serialize(new { 
                    Title = ticket.Title,
                    Priority = ticket.Priority.ToString(),
                    Category = ticket.CategoryId
                }),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            // Auto-categorize in background (don't await to avoid blocking)
            _ = Task.Run(async () => await AutoCategorizeTicketAsync(ticket.Id));

            return ticket;
        }

        public async Task<Ticket?> GetTicketAsync(int ticketId)
        {
            return await _unitOfWork.Tickets.GetTicketWithDetailsAsync(ticketId);
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByUserAsync(int userId, UserRole userRole)
        {
            return userRole switch
            {
                UserRole.Customer => await _unitOfWork.Tickets.GetTicketsByCustomerAsync(userId),
                UserRole.Agent => await _unitOfWork.Tickets.GetTicketsByAgentAsync(userId),
                UserRole.Admin => await _unitOfWork.Tickets.GetAllAsync(),
                _ => new List<Ticket>()
            };
        }

        public async Task<Ticket> UpdateTicketStatusAsync(int ticketId, TicketStatus status, int userId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new ArgumentException("Ticket not found");

            var oldStatus = ticket.Status;
            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;

            // Set resolved/closed timestamps
            if (status == TicketStatus.Resolved && ticket.ResolvedAt == null)
                ticket.ResolvedAt = DateTime.UtcNow;
            else if (status == TicketStatus.Closed && ticket.ClosedAt == null)
                ticket.ClosedAt = DateTime.UtcNow;

            _unitOfWork.Tickets.Update(ticket);

            // Create history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "StatusChanged",
                OldValue = oldStatus.ToString(),
                NewValue = status.ToString(),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            return ticket;
        }

        public async Task<Ticket> AssignTicketAsync(int ticketId, int agentId, int assignedByUserId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new ArgumentException("Ticket not found");

            var agent = await _unitOfWork.Users.GetByIdAsync(agentId);
            if (agent == null || agent.Role != UserRole.Agent)
                throw new ArgumentException("Invalid agent");

            var oldAgentId = ticket.AssignedAgentId;
            ticket.AssignedAgentId = agentId;
            ticket.UpdatedAt = DateTime.UtcNow;

            if (ticket.Status == TicketStatus.New)
                ticket.Status = TicketStatus.InProgress;

            _unitOfWork.Tickets.Update(ticket);

            // Create history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticketId,
                UserId = assignedByUserId,
                Action = "Assigned",
                OldValue = oldAgentId?.ToString(),
                NewValue = agentId.ToString(),
                Details = JsonSerializer.Serialize(new { 
                    AgentName = agent.FullName 
                }),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            return ticket;
        }

        public async Task<TicketComment> AddCommentAsync(int ticketId, int userId, string comment, bool isInternal = false)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new ArgumentException("Ticket not found");

            var ticketComment = new TicketComment
            {
                TicketId = ticketId,
                UserId = userId,
                CommentText = comment,
                IsInternal = isInternal,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TicketComments.AddAsync(ticketComment);

            // Update ticket timestamp
            ticket.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Tickets.Update(ticket);

            // Create history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "CommentAdded",
                NewValue = isInternal ? "Internal Comment" : "Comment",
                Details = JsonSerializer.Serialize(new { 
                    CommentLength = comment.Length,
                    IsInternal = isInternal
                }),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            return ticketComment;
        }

        public async Task<IEnumerable<Ticket>> SearchTicketsAsync(string searchTerm)
        {
            return await _unitOfWork.Tickets.SearchTicketsAsync(searchTerm);
        }

        public async Task<Ticket> UpdateTicketPriorityAsync(int ticketId, Priority priority, int userId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new ArgumentException("Ticket not found");

            var oldPriority = ticket.Priority;
            ticket.Priority = priority;
            ticket.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tickets.Update(ticket);

            // Create history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "PriorityChanged",
                OldValue = oldPriority.ToString(),
                NewValue = priority.ToString(),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            return ticket;
        }

        public async Task AutoCategorizeTicketAsync(int ticketId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null) return;

            try
            {
                // Use AI to categorize
                var categoryName = await _aiService.CategorizeSupportTicketAsync(ticket.Title, ticket.Description);
                var priority = await _aiService.AnalyzePriorityAsync(ticket.Title, ticket.Description);
                var sentiment = await _aiService.AnalyzeSentimentAsync($"{ticket.Title} {ticket.Description}");

                // Find matching category
                var categories = await _unitOfWork.Categories.GetAllAsync();
                var matchingCategory = categories.FirstOrDefault(c => 
                    c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                // Update ticket with AI insights
                if (matchingCategory != null && ticket.CategoryId == null)
                {
                    ticket.CategoryId = matchingCategory.Id;
                }

                if (ticket.Priority == Priority.Medium) // Only update if still default
                {
                    ticket.Priority = (Priority)priority;
                }

                // Store AI analysis
                ticket.AIAnalysis = JsonSerializer.Serialize(new
                {
                    SuggestedCategory = categoryName,
                    SuggestedPriority = priority,
                    SentimentScore = sentiment,
                    AnalyzedAt = DateTime.UtcNow
                });

                _unitOfWork.Tickets.Update(ticket);

                // Create AI insights
                var insights = new List<AIInsight>
                {
                    await _aiService.CreateInsightAsync(ticketId, "Categorization", new { Category = categoryName }, 0.8),
                    await _aiService.CreateInsightAsync(ticketId, "Priority", new { Priority = priority }, 0.7),
                    await _aiService.CreateInsightAsync(ticketId, "Sentiment", new { Sentiment = sentiment }, 0.9)
                };

                await _unitOfWork.AIInsights.AddRangeAsync(insights);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Silent fail for AI categorization
            }
        }

        public async Task<User?> SuggestBestAgentAsync(int ticketId)
        {
            var ticket = await _unitOfWork.Tickets.GetTicketWithDetailsAsync(ticketId);
            if (ticket == null) return null;

            try
            {
                var availableAgents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                if (!availableAgents.Any()) return null;

                var suggestedAgentId = await _aiService.SuggestBestAgentAsync(
                    $"{ticket.Title}\n{ticket.Description}", 
                    availableAgents
                );

                return availableAgents.FirstOrDefault(a => a.Id == suggestedAgentId);
            }
            catch
            {
                // Return agent with least workload as fallback
                var agents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                return agents.FirstOrDefault();
            }
        }
    }
}
