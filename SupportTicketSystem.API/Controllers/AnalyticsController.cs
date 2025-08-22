using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public AnalyticsController(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboardAnalytics()
        {
            try
            {
                var analytics = new
                {
                    // Overview Stats
                    TotalTickets = await _context.Tickets.CountAsync(),
                    OpenTickets = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.New || t.Status == TicketStatus.InProgress),
                    ResolvedToday = await _context.Tickets.CountAsync(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == DateTime.UtcNow.Date),
                    OverdueTickets = await _context.Tickets.CountAsync(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow && t.Status != TicketStatus.Closed),

                    // Ticket Distribution
                    TicketsByStatus = await _context.Tickets
                        .GroupBy(t => t.Status)
                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),

                    TicketsByPriority = await _context.Tickets
                        .GroupBy(t => t.Priority)
                        .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),

                    TicketsByCategory = await _context.Tickets
                        .Include(t => t.Category)
                        .Where(t => t.Category != null)
                        .GroupBy(t => t.Category.Name)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToListAsync(),

                    // Agent Workload (simplified)
                    AgentWorkload = await GetAgentWorkloadAsync(),

                    // Trends (Last 7 days)
                    TicketTrends = await GetTicketTrendsAsync(),

                    // AI Insights Summary
                    AIInsightsSummary = await _context.AIInsights
                        .GroupBy(ai => ai.InsightType)
                        .Select(g => new { InsightType = g.Key.ToString(), Count = g.Count(), AvgConfidence = g.Average(ai => ai.Confidence) })
                        .ToListAsync()
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve analytics", error = ex.Message });
            }
        }

        [HttpGet("agent-performance")]
        public async Task<ActionResult> GetAgentPerformance()
        {
            try
            {
                var agents = await _context.Users
                    .Where(u => u.Role == UserRole.Agent)
                    .Include(u => u.AgentSkills)
                        .ThenInclude(as_ => as_.Skill)
                    .ToListAsync();

                var agentPerformance = new List<object>();

                foreach (var agent in agents)
                {
                    // Get agent's tickets
                    var agentTickets = await _context.Tickets
                        .Where(t => t.AssignedAgentId == agent.Id)
                        .ToListAsync();

                    var resolvedTickets = agentTickets
                        .Where(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                        .ToList();

                    var thisMonthResolved = resolvedTickets
                        .Where(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Month == DateTime.UtcNow.Month)
                        .ToList();

                    // Calculate average resolution time
                    var avgResolutionHours = resolvedTickets
                        .Where(t => t.ResolvedAt.HasValue)
                        .Select(t => (t.ResolvedAt.Value - t.CreatedAt).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average();

                    // Get AI sentiment scores for agent's tickets
                    var sentimentScores = await _context.AIInsights
                        .Where(ai => agentTickets.Select(t => t.Id).Contains(ai.TicketId) && ai.InsightType == InsightType.Sentiment)
                        .Select(ai => ai.Confidence)
                        .ToListAsync();

                    var performance = new
                    {
                        AgentId = agent.Id,
                        AgentName = agent.FullName,
                        Email = agent.Email,
                        Skills = agent.AgentSkills.Select(s => new { s.Skill.Name, s.ProficiencyLevel }).ToList(),
                        
                        // Ticket Statistics
                        TotalAssigned = agentTickets.Count,
                        CurrentlyAssigned = agentTickets.Count(t => t.Status == TicketStatus.InProgress),
                        ResolvedTotal = resolvedTickets.Count,
                        ResolvedThisMonth = thisMonthResolved.Count,

                        // Performance Metrics
                        AvgResolutionTimeHours = Math.Round(avgResolutionHours, 2),
                        CustomerSatisfactionScore = sentimentScores.Any() ? Math.Round(sentimentScores.Average(), 2) : 0.5,

                        LastActivity = agent.LastLoginAt,
                        IsActive = agent.IsActive
                    };

                    agentPerformance.Add(performance);
                }

                return Ok(agentPerformance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve agent performance", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("customer-insights")]
        public async Task<ActionResult> GetCustomerInsights()
        {
            try
            {
                var customers = await _context.Users
                    .Where(u => u.Role == UserRole.Customer)
                    .ToListAsync();

                var customerInsights = new List<object>();

                foreach (var customer in customers)
                {
                    var customerTickets = await _context.Tickets
                        .Where(t => t.CustomerId == customer.Id)
                        .Include(t => t.Category)
                        .ToListAsync();

                    var mostUsedCategories = customerTickets
                        .Where(t => t.Category != null)
                        .GroupBy(t => t.Category.Name)
                        .OrderByDescending(g => g.Count())
                        .Take(3)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .ToList();

                    var avgPriority = customerTickets.Any() 
                        ? customerTickets.Select(t => (int)t.Priority).Average()
                        : 2.0;

                    var lastTicketDate = customerTickets
                        .OrderByDescending(t => t.CreatedAt)
                        .FirstOrDefault()?.CreatedAt;

                    // Get sentiment scores for customer's tickets
                    var sentimentScores = await _context.AIInsights
                        .Where(ai => customerTickets.Select(t => t.Id).Contains(ai.TicketId) && ai.InsightType == InsightType.Sentiment)
                        .Select(ai => ai.Confidence)
                        .ToListAsync();

                    var insight = new
                    {
                        CustomerId = customer.Id,
                        CustomerName = customer.FullName,
                        Email = customer.Email,
                        
                        // Ticket Statistics
                        TotalTickets = customerTickets.Count,
                        OpenTickets = customerTickets.Count(t => t.Status == TicketStatus.New || t.Status == TicketStatus.InProgress),
                        ResolvedTickets = customerTickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
                        
                        // Behavior Analysis
                        MostUsedCategories = mostUsedCategories,
                        AvgPriorityLevel = Math.Round(avgPriority, 2),
                        LastTicketDate = lastTicketDate,

                        // Satisfaction Metrics
                        OverallSentiment = sentimentScores.Any() ? Math.Round(sentimentScores.Average(), 2) : 0.5,
                        JoinDate = customer.CreatedAt
                    };

                    customerInsights.Add(insight);
                }

                return Ok(customerInsights.OrderByDescending(c => ((dynamic)c).TotalTickets).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve customer insights", error = ex.Message });
            }
        }

        private async Task<object> GetAgentWorkloadAsync()
        {
            var agents = await _context.Users
                .Where(u => u.Role == UserRole.Agent)
                .ToListAsync();

            var workload = new List<object>();

            foreach (var agent in agents)
            {
                var assignedCount = await _context.Tickets
                    .CountAsync(t => t.AssignedAgentId == agent.Id && t.Status != TicketStatus.Closed);

                var resolvedThisWeek = await _context.Tickets
                    .CountAsync(t => t.AssignedAgentId == agent.Id && 
                                   t.ResolvedAt.HasValue && 
                                   t.ResolvedAt.Value >= DateTime.UtcNow.AddDays(-7));

                workload.Add(new
                {
                    AgentName = agent.FullName,
                    AssignedTickets = assignedCount,
                    ResolvedThisWeek = resolvedThisWeek
                });
            }

            return workload;
        }

        private async Task<object> GetTicketTrendsAsync()
        {
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Reverse()
                .ToList();

            var trends = new List<object>();
            
            foreach (var date in last7Days)
            {
                var dayStats = new
                {
                    Date = date,
                    Created = await _context.Tickets.CountAsync(t => t.CreatedAt.Date == date),
                    Resolved = await _context.Tickets.CountAsync(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == date),
                    InProgress = await _context.Tickets.CountAsync(t => t.UpdatedAt.Date == date && t.Status == TicketStatus.InProgress)
                };
                trends.Add(dayStats);
            }

            return trends;
        }
    }
}
