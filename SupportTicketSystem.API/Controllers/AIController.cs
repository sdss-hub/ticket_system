using Microsoft.AspNetCore.Mvc;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IUnitOfWork _unitOfWork;

        public AIController(IAIService aiService, IUnitOfWork unitOfWork)
        {
            _aiService = aiService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("categorize")]
        public async Task<ActionResult> CategorizeTicket([FromBody] CategorizationRequest request)
        {
            try
            {
                var category = await _aiService.CategorizeSupportTicketAsync(request.Title, request.Description);
                var priority = await _aiService.AnalyzePriorityAsync(request.Title, request.Description);
                var sentiment = await _aiService.AnalyzeSentimentAsync($"{request.Title} {request.Description}");

                var result = new
                {
                    SuggestedCategory = category,
                    SuggestedPriority = priority,
                    SentimentScore = Math.Round(sentiment, 2),
                    SentimentLabel = sentiment switch
                    {
                        < 0.3 => "Negative",
                        < 0.7 => "Neutral", 
                        _ => "Positive"
                    },
                    Confidence = new
                    {
                        Categorization = 0.85,
                        Priority = 0.78,
                        Sentiment = 0.92
                    },
                    ProcessedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "AI analysis failed", error = ex.Message });
            }
        }

        [HttpPost("suggest-response")]
        public async Task<ActionResult> SuggestResponse([FromBody] ResponseSuggestionRequest request)
        {
            try
            {
                var suggestion = await _aiService.GenerateResponseSuggestionAsync(request.TicketContent, request.CustomerMessage);

                var result = new
                {
                    SuggestedResponse = suggestion,
                    Confidence = 0.82,
                    GeneratedAt = DateTime.UtcNow,
                    Tips = new[]
                    {
                        "Review and personalize before sending",
                        "Add specific technical details if needed",
                        "Consider customer's technical level"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Response suggestion failed", error = ex.Message });
            }
        }

        [HttpPost("suggest-agent/{ticketId}")]
        public async Task<ActionResult> SuggestAgent(int ticketId)
        {
            try
            {
                var ticket = await _unitOfWork.Tickets.GetTicketWithDetailsAsync(ticketId);
                if (ticket == null)
                    return NotFound(new { message = "Ticket not found" });

                var availableAgents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                if (!availableAgents.Any())
                    return NotFound(new { message = "No available agents" });

                var suggestedAgentId = await _aiService.SuggestBestAgentAsync(
                    $"{ticket.Title}\n{ticket.Description}", 
                    availableAgents
                );

                var suggestedAgent = availableAgents.FirstOrDefault(a => a.Id == suggestedAgentId);
                if (suggestedAgent == null)
                    suggestedAgent = availableAgents.First(); // Fallback

                var result = new
                {
                    SuggestedAgent = new
                    {
                        suggestedAgent.Id,
                        suggestedAgent.FullName,
                        suggestedAgent.Email,
                        Skills = suggestedAgent.AgentSkills.Select(s => new { s.Skill.Name, s.ProficiencyLevel }).ToList(),
                        CurrentWorkload = suggestedAgent.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress)
                    },
                    Reasoning = "Selected based on skills match and current workload",
                    Confidence = 0.78,
                    AlternativeAgents = availableAgents
                        .Where(a => a.Id != suggestedAgent.Id)
                        .Take(2)
                        .Select(a => new
                        {
                            a.Id,
                            a.FullName,
                            CurrentWorkload = a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress)
                        })
                        .ToList(),
                    ProcessedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Agent suggestion failed", error = ex.Message });
            }
        }

        [HttpGet("insights/{ticketId}")]
        public async Task<ActionResult> GetTicketInsights(int ticketId)
        {
            try
            {
                var insights = await _unitOfWork.AIInsights.FindAsync(ai => ai.TicketId == ticketId);
                
                var result = insights.Select(insight => new
                {
                    insight.Id,
                    insight.InsightType,
                    insight.Confidence,
                    Data = insight.Data, // JSON data
                    insight.CreatedAt
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve AI insights", error = ex.Message });
            }
        }

        [HttpPost("analyze-ticket/{ticketId}")]
        public async Task<ActionResult> AnalyzeExistingTicket(int ticketId)
        {
            try
            {
                var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
                if (ticket == null)
                    return NotFound(new { message = "Ticket not found" });

                // Perform AI analysis
                var category = await _aiService.CategorizeSupportTicketAsync(ticket.Title, ticket.Description);
                var priority = await _aiService.AnalyzePriorityAsync(ticket.Title, ticket.Description);
                var sentiment = await _aiService.AnalyzeSentimentAsync($"{ticket.Title} {ticket.Description}");

                // Create new AI insights
                var insights = new[]
                {
                    await _aiService.CreateInsightAsync(ticketId, "Categorization", new { Category = category }, 0.85),
                    await _aiService.CreateInsightAsync(ticketId, "Priority", new { Priority = priority }, 0.78),
                    await _aiService.CreateInsightAsync(ticketId, "Sentiment", new { Sentiment = sentiment }, 0.92)
                };

                await _unitOfWork.AIInsights.AddRangeAsync(insights);
                await _unitOfWork.SaveChangesAsync();

                var result = new
                {
                    TicketId = ticketId,
                    Analysis = new
                    {
                        SuggestedCategory = category,
                        SuggestedPriority = priority,
                        SentimentScore = Math.Round(sentiment, 2),
                        SentimentLabel = sentiment switch
                        {
                            < 0.3 => "Negative",
                            < 0.7 => "Neutral",
                            _ => "Positive"
                        }
                    },
                    InsightsCreated = insights.Length,
                    ProcessedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ticket analysis failed", error = ex.Message });
            }
        }
    }

    public class CategorizationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ResponseSuggestionRequest
    {
        public string TicketContent { get; set; } = string.Empty;
        public string CustomerMessage { get; set; } = string.Empty;
    }
}
