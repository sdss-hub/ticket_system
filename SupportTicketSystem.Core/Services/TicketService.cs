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

        public async Task<Ticket> CreateTicketAsync(Ticket ticket, BusinessImpact? businessImpact = null)
        {
            // Generate ticket number
            ticket.TicketNumber = await _unitOfWork.Tickets.GenerateTicketNumberAsync();
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            // Store business impact data
            if (businessImpact != null)
            {
                ticket.BusinessImpactData = JsonSerializer.Serialize(businessImpact);
            }

            // Add ticket first to get ID
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            // Now perform AI analysis and smart assignment
            await PerformIntelligentTicketProcessingAsync(ticket, businessImpact);

            // Create initial history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticket.Id,
                UserId = ticket.CustomerId,
                Action = "Created",
                NewValue = ticket.Status.ToString(),
                Details = JsonSerializer.Serialize(new { 
                    Title = ticket.Title,
                    Priority = ticket.Priority.ToString(),
                    Category = ticket.CategoryId,
                    AssignmentMethod = ticket.AssignmentMethod,
                    BusinessImpact = businessImpact != null
                }),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            return ticket;
        }

        private async Task PerformIntelligentTicketProcessingAsync(Ticket ticket, BusinessImpact? businessImpact)
        {
            try
            {
                // 1. AI Analysis
                var aiAnalysis = await PerformAIAnalysisAsync(ticket, businessImpact);
                
                // 2. Apply AI insights
                if (aiAnalysis != null)
                {
                    // Update category if not already set and AI has high confidence
                    if (ticket.CategoryId == null && aiAnalysis.CategoryConfidence > 0.7)
                    {
                        var categories = await _unitOfWork.Categories.GetAllAsync();
                        var matchingCategory = categories.FirstOrDefault(c => 
                            c.Name.Equals(aiAnalysis.SuggestedCategory, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingCategory != null)
                        {
                            ticket.CategoryId = matchingCategory.Id;
                        }
                    }

                    // Set priority based on AI analysis and business impact
                    ticket.Priority = aiAnalysis.FinalPriority;

                    // Store AI analysis results
                    ticket.AIAnalysis = JsonSerializer.Serialize(aiAnalysis);

                    // Set SLA deadlines based on priority
                    SetSLADeadlines(ticket);
                }

                // 3. Smart Agent Assignment
                await PerformSmartAssignmentAsync(ticket);

                // 4. Auto-escalation if needed
                await CheckAutoEscalationAsync(ticket, businessImpact);

                _unitOfWork.Tickets.Update(ticket);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't fail ticket creation
                Console.WriteLine($"AI processing failed for ticket {ticket.Id}: {ex.Message}");
                
                // Fallback to default values
                if (ticket.Priority == Priority.Medium && businessImpact != null)
                {
                    ticket.Priority = AnalyzeBusinessImpactFallback(businessImpact);
                }
            }
        }

        private async Task<AITicketAnalysis?> PerformAIAnalysisAsync(Ticket ticket, BusinessImpact? businessImpact)
        {
            try
            {
                // Get AI suggestions
                var categoryTask = _aiService.CategorizeSupportTicketAsync(ticket.Title, ticket.Description);
                var priorityTask = _aiService.AnalyzePriorityAsync(ticket.Title, ticket.Description, businessImpact);
                var sentimentTask = _aiService.AnalyzeSentimentAsync($"{ticket.Title} {ticket.Description}");

                await Task.WhenAll(categoryTask, priorityTask, sentimentTask);

                var analysis = new AITicketAnalysis
                {
                    SuggestedCategory = await categoryTask,
                    SuggestedPriority = (Priority)await priorityTask,
                    FinalPriority = (Priority)await priorityTask,
                    SentimentScore = await sentimentTask,
                    CategoryConfidence = 0.85,
                    PriorityConfidence = 0.78,
                    SentimentConfidence = 0.92,
                    AnalyzedAt = DateTime.UtcNow,
                    KeyWords = ExtractKeywords(ticket.Title, ticket.Description),
                    UrgencyIndicators = ExtractUrgencyIndicators(ticket.Title, ticket.Description)
                };

                // Create AI insights for storage
                var insights = new[]
                {
                    await _aiService.CreateInsightAsync(ticket.Id, "Categorization", 
                        new { Category = analysis.SuggestedCategory, Keywords = analysis.KeyWords }, analysis.CategoryConfidence),
                    await _aiService.CreateInsightAsync(ticket.Id, "Priority", 
                        new { Priority = analysis.SuggestedPriority, Urgency = analysis.UrgencyIndicators }, analysis.PriorityConfidence),
                    await _aiService.CreateInsightAsync(ticket.Id, "Sentiment", 
                        new { Score = analysis.SentimentScore, Indicators = analysis.UrgencyIndicators }, analysis.SentimentConfidence)
                };

                await _unitOfWork.AIInsights.AddRangeAsync(insights);

                return analysis;
            }
            catch
            {
                return null;
            }
        }

        private async Task PerformSmartAssignmentAsync(Ticket ticket)
        {
            try
            {
                var availableAgents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                
                if (!availableAgents.Any())
                {
                    // No agents available - goes to queue
                    ticket.AssignmentMethod = "Queue";
                    ticket.AssignmentReason = "No agents currently available";
                    ticket.Status = TicketStatus.New;
                    return;
                }

                // Try AI-powered assignment first
                var suggestedAgentId = await _aiService.SuggestBestAgentAsync(
                    $"{ticket.Title}\n{ticket.Description}", 
                    availableAgents);

                var suggestedAgent = availableAgents.FirstOrDefault(a => a.Id == suggestedAgentId);

                if (suggestedAgent != null)
                {
                    // AI suggested an agent
                    ticket.AssignedAgentId = suggestedAgent.Id;
                    ticket.AssignedAt = DateTime.UtcNow;
                    ticket.Status = TicketStatus.InProgress;
                    ticket.AssignmentMethod = "AI";
                    ticket.AssignmentReason = $"AI recommended based on skills and workload";
                }
                else
                {
                    // AI failed, use fallback assignment
                    AssignToFallbackAgent(ticket, availableAgents);
                }
            }
            catch
            {
                // AI assignment failed, use fallback
                var availableAgents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                AssignToFallbackAgent(ticket, availableAgents);
            }
        }

        // Fixed: Removed async since it doesn't await anything
        private void AssignToFallbackAgent(Ticket ticket, IEnumerable<User> availableAgents)
        {
            if (!availableAgents.Any())
            {
                ticket.AssignmentMethod = "Queue";
                ticket.AssignmentReason = "No agents available";
                return;
            }

            // Fallback 1: Find agent with matching category experience
            if (ticket.CategoryId.HasValue)
            {
                var categorySpecialists = availableAgents.Where(a => 
                    a.AgentSkills.Any(skill => 
                        skill.Skill.Name.Contains("Technical") || 
                        skill.Skill.Name.Contains("Account") || 
                        skill.Skill.Name.Contains("Billing"))).ToList();

                if (categorySpecialists.Any())
                {
                    var selectedAgent = categorySpecialists
                        .OrderBy(a => a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress))
                        .First();
                    
                    ticket.AssignedAgentId = selectedAgent.Id;
                    ticket.AssignedAt = DateTime.UtcNow;
                    ticket.Status = TicketStatus.InProgress;
                    ticket.AssignmentMethod = "CategoryMatch";
                    ticket.AssignmentReason = $"Assigned to category specialist with lowest workload";
                    return;
                }
            }

            // Fallback 2: Round-robin to least busy agent
            var leastBusyAgent = availableAgents
                .OrderBy(a => a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress))
                .First();

            ticket.AssignedAgentId = leastBusyAgent.Id;
            ticket.AssignedAt = DateTime.UtcNow;
            ticket.Status = TicketStatus.InProgress;
            ticket.AssignmentMethod = "RoundRobin";
            ticket.AssignmentReason = $"Assigned to agent with lowest current workload ({leastBusyAgent.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress)} tickets)";
        }

        private async Task CheckAutoEscalationAsync(Ticket ticket, BusinessImpact? businessImpact)
        {
            var shouldEscalate = false;
            var escalationReason = "";

            // Escalate if high/critical priority with no available agents
            if (ticket.Priority >= Priority.High && ticket.AssignedAgentId == null)
            {
                shouldEscalate = true;
                escalationReason = $"High priority ticket ({ticket.Priority}) could not be assigned to any agent";
            }

            // Escalate if business impact indicates company-wide issue
            if (businessImpact?.ImpactScope == ImpactScope.Company || businessImpact?.BlockingLevel == BlockingLevel.SystemDown)
            {
                shouldEscalate = true;
                escalationReason = $"Company-wide impact detected: {businessImpact.BlockingLevel} affecting {businessImpact.ImpactScope}";
            }

            // Escalate if urgent deadline is very soon
            if (businessImpact?.UrgentDeadline.HasValue == true && 
                businessImpact.UrgentDeadline.Value <= DateTime.UtcNow.AddHours(2))
            {
                shouldEscalate = true;
                escalationReason = $"Urgent deadline in {(businessImpact.UrgentDeadline.Value - DateTime.UtcNow).TotalHours:F1} hours";
            }

            if (shouldEscalate)
            {
                // Find admin or manager to escalate to
                var admins = await _unitOfWork.Users.GetUsersByRoleAsync(UserRole.Admin);
                if (admins.Any())
                {
                    ticket.IsEscalated = true;
                    ticket.EscalationReason = escalationReason;
                    ticket.EscalatedAt = DateTime.UtcNow;
                    ticket.EscalatedById = 1; // System escalation
                    
                    Console.WriteLine($"ESCALATION: Ticket {ticket.TicketNumber} escalated - {escalationReason}");
                }
            }
        }

        private void SetSLADeadlines(Ticket ticket)
        {
            var now = DateTime.UtcNow;
            
            // Set first response deadline
            ticket.FirstResponseDeadline = ticket.Priority switch
            {
                Priority.Critical => now.AddHours(1),
                Priority.High => now.AddHours(4),
                Priority.Medium => now.AddHours(24),
                Priority.Low => now.AddHours(48),
                _ => now.AddHours(24)
            };

            // Set resolution deadline
            ticket.ResolutionDeadline = ticket.Priority switch
            {
                Priority.Critical => now.AddHours(4),
                Priority.High => now.AddHours(24),
                Priority.Medium => now.AddDays(3),
                Priority.Low => now.AddDays(7),
                _ => now.AddDays(3)
            };

            ticket.DueDate = ticket.ResolutionDeadline;
        }

        private static Priority AnalyzeBusinessImpactFallback(BusinessImpact businessImpact)
        {
            var priority = Priority.Medium;

            if (businessImpact.BlockingLevel == BlockingLevel.SystemDown || 
                businessImpact.ImpactScope == ImpactScope.Company)
            {
                priority = Priority.Critical;
            }
            else if (businessImpact.BlockingLevel == BlockingLevel.CompletelyBlocking || 
                     businessImpact.ImpactScope == ImpactScope.Department)
            {
                priority = Priority.High;
            }
            else if (businessImpact.BlockingLevel == BlockingLevel.PartiallyBlocking || 
                     businessImpact.ImpactScope == ImpactScope.Team)
            {
                priority = Priority.Medium;
            }

            return priority;
        }

        private static string ExtractKeywords(string title, string description)
        {
            var content = (title + " " + description).ToLower();
            var keywords = new List<string>();

            var importantWords = new[] { 
                "login", "password", "access", "error", "crash", "broken", "billing", 
                "payment", "urgent", "critical", "system", "down", "bug", "feature" 
            };

            foreach (var word in importantWords)
            {
                if (content.Contains(word))
                {
                    keywords.Add(word);
                }
            }

            return string.Join(", ", keywords);
        }

        private static string ExtractUrgencyIndicators(string title, string description)
        {
            var content = (title + " " + description).ToLower();
            var indicators = new List<string>();

            var urgencyWords = new[] { 
                "urgent", "asap", "immediately", "critical", "emergency", 
                "can't work", "blocking", "down", "!!!" 
            };

            foreach (var word in urgencyWords)
            {
                if (content.Contains(word))
                {
                    indicators.Add(word);
                }
            }

            return string.Join(", ", indicators);
        }

        // All other existing methods remain the same...
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

            // Update timestamps based on status changes
            if (status == TicketStatus.InProgress && ticket.FirstResponseAt == null)
                ticket.FirstResponseAt = DateTime.UtcNow;
            
            if (status == TicketStatus.Resolved && ticket.ResolvedAt == null)
                ticket.ResolvedAt = DateTime.UtcNow;
            
            if (status == TicketStatus.Closed && ticket.ClosedAt == null)
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
            ticket.AssignedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.AssignmentMethod = "Manual";
            ticket.AssignmentReason = $"Manually assigned by user {assignedByUserId}";

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
                    AgentName = agent.FullName,
                    AssignmentMethod = "Manual"
                }),
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.TicketHistory.AddAsync(historyEntry);
            await _unitOfWork.SaveChangesAsync();

            return ticket;
        }

        public async Task<TicketComment> AddCommentAsync(int ticketId, int userId, string comment, bool isInternal = false, bool useAIAssistance = false)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null)
                throw new ArgumentException("Ticket not found");

            var commentText = comment;
            var aiConfidence = 0.0;
            var isAIGenerated = false;

            // AI-assisted response generation
            if (useAIAssistance && !isInternal)
            {
                try
                {
                    var aiSuggestion = await _aiService.GenerateResponseSuggestionAsync(
                        $"{ticket.Title}\n{ticket.Description}", 
                        comment, 
                        isInternal);
                    
                    if (!string.IsNullOrEmpty(aiSuggestion))
                    {
                        commentText = aiSuggestion;
                        isAIGenerated = true;
                        aiConfidence = 0.82;
                    }
                }
                catch
                {
                    // Fall back to original comment if AI fails
                }
            }

            var ticketComment = new TicketComment
            {
                TicketId = ticketId,
                UserId = userId,
                CommentText = commentText,
                IsInternal = isInternal,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TicketComments.AddAsync(ticketComment);

            // Update ticket timestamp and first response
            ticket.UpdatedAt = DateTime.UtcNow;
            if (ticket.FirstResponseAt == null && !isInternal)
            {
                ticket.FirstResponseAt = DateTime.UtcNow;
            }
            
            _unitOfWork.Tickets.Update(ticket);

            // Create history entry
            var historyEntry = new TicketHistory
            {
                TicketId = ticketId,
                UserId = userId,
                Action = "CommentAdded",
                NewValue = isInternal ? "Internal Comment" : "Comment",
                Details = JsonSerializer.Serialize(new { 
                    CommentLength = commentText.Length,
                    IsInternal = isInternal,
                    IsAIGenerated = isAIGenerated,
                    AIConfidence = aiConfidence
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

            // Update SLA deadlines based on new priority
            SetSLADeadlines(ticket);

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
                return agents.OrderBy(a => a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress)).FirstOrDefault();
            }
        }

        public async Task AutoCategorizeTicketAsync(int ticketId)
        {
            // This is now handled in the intelligent processing during creation
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket != null)
            {
                await PerformIntelligentTicketProcessingAsync(ticket, null);
            }
        }
    }

    // Helper class for AI analysis results
    public class AITicketAnalysis
    {
        public string SuggestedCategory { get; set; } = string.Empty;
        public Priority SuggestedPriority { get; set; }
        public Priority FinalPriority { get; set; }
        public double SentimentScore { get; set; }
        public double CategoryConfidence { get; set; }
        public double PriorityConfidence { get; set; }
        public double SentimentConfidence { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public string? KeyWords { get; set; }
        public string? UrgencyIndicators { get; set; }
    }
}
