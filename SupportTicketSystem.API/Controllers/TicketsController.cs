using Microsoft.AspNetCore.Mvc;
using SupportTicketSystem.API.DTOs;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;
using System.Text.Json;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IUnitOfWork _unitOfWork;

        public TicketsController(ITicketService ticketService, IUnitOfWork unitOfWork)
        {
            _ticketService = ticketService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketResponseDto>>> GetTickets(
            [FromQuery] TicketStatus? status = null,
            [FromQuery] int? customerId = null,
            [FromQuery] int? agentId = null,
            [FromQuery] string? search = null,
            [FromQuery] bool includeEscalated = false)
        {
            try
            {
                // Get tickets with all necessary includes
                var query = _unitOfWork.Tickets.GetAllAsync();
                var allTickets = await query;
                
                // Apply filters
                var tickets = allTickets.AsQueryable();
                
                if (status.HasValue)
                    tickets = tickets.Where(t => t.Status == status.Value);
                
                if (customerId.HasValue)
                    tickets = tickets.Where(t => t.CustomerId == customerId.Value);
                
                if (agentId.HasValue)
                    tickets = tickets.Where(t => t.AssignedAgentId == agentId.Value);
                
                if (!string.IsNullOrEmpty(search))
                    tickets = tickets.Where(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                               t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                               t.TicketNumber.Contains(search));

                if (includeEscalated)
                    tickets = tickets.Where(t => t.IsEscalated);

                var result = new List<TicketResponseDto>();
                
                foreach (var ticket in tickets.OrderByDescending(t => t.CreatedAt).Take(100))
                {
                    // Load related entities manually if needed
                    var customer = await _unitOfWork.Users.GetByIdAsync(ticket.CustomerId);
                    var assignedAgent = ticket.AssignedAgentId.HasValue 
                        ? await _unitOfWork.Users.GetByIdAsync(ticket.AssignedAgentId.Value) 
                        : null;
                    var category = ticket.CategoryId.HasValue 
                        ? await _unitOfWork.Categories.GetByIdAsync(ticket.CategoryId.Value) 
                        : null;

                    var ticketDto = await MapToTicketResponseDtoAsync(ticket, customer, assignedAgent, category);
                    result.Add(ticketDto);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving tickets", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TicketResponseDto>> GetTicket(int id)
        {
            try
            {
                var ticket = await _ticketService.GetTicketAsync(id);
                if (ticket == null)
                {
                    return NotFound(new { message = "Ticket not found" });
                }

                var ticketDto = await MapToTicketResponseDtoAsync(ticket);
                return Ok(ticketDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the ticket", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TicketResponseDto>> CreateTicket(CreateTicketDto createTicketDto)
        {
            try
            {
                // Validate customer exists
                var customer = await _unitOfWork.Users.GetByIdAsync(createTicketDto.CustomerId);
                if (customer == null || customer.Role != UserRole.Customer)
                {
                    return BadRequest(new { message = "Invalid customer ID" });
                }

                var ticket = new Ticket
                {
                    Title = createTicketDto.Title,
                    Description = createTicketDto.Description,
                    CustomerId = createTicketDto.CustomerId,
                    CategoryId = createTicketDto.CategoryId,
                    Status = TicketStatus.New,
                    Priority = Priority.Medium // Will be overridden by AI analysis
                };

                // Create ticket with intelligent processing
                var createdTicket = await _ticketService.CreateTicketAsync(ticket, createTicketDto.BusinessImpact?.ToBusinessImpact());
                var fullTicket = await _ticketService.GetTicketAsync(createdTicket.Id);
                
                if (fullTicket == null)
                {
                    return StatusCode(500, new { message = "Failed to retrieve created ticket" });
                }

                var ticketDto = await MapToTicketResponseDtoAsync(fullTicket);
                
                return CreatedAtAction(nameof(GetTicket), new { id = ticketDto.Id }, new
                {
                    ticket = ticketDto,
                    message = "Ticket created successfully",
                    aiProcessed = !string.IsNullOrEmpty(fullTicket.AIAnalysis),
                    assignmentInfo = new
                    {
                        method = fullTicket.AssignmentMethod,
                        reason = fullTicket.AssignmentReason,
                        isEscalated = fullTicket.IsEscalated
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the ticket", error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<TicketResponseDto>> UpdateTicketStatus(int id, [FromBody] TicketStatus status, [FromQuery] int userId = 1)
        {
            try
            {
                var updatedTicket = await _ticketService.UpdateTicketStatusAsync(id, status, userId);
                var fullTicket = await _ticketService.GetTicketAsync(updatedTicket.Id);
                
                if (fullTicket == null)
                {
                    return StatusCode(500, new { message = "Failed to retrieve updated ticket" });
                }

                var ticketDto = await MapToTicketResponseDtoAsync(fullTicket);
                return Ok(new
                {
                    ticket = ticketDto,
                    message = $"Ticket status updated to {status}",
                    slaInfo = new
                    {
                        firstResponseDeadline = fullTicket.FirstResponseDeadline,
                        resolutionDeadline = fullTicket.ResolutionDeadline,
                        isOverdue = fullTicket.ResolutionDeadline < DateTime.UtcNow && status != TicketStatus.Closed
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the ticket status", error = ex.Message });
            }
        }

        [HttpPut("{id}/assign")]
        public async Task<ActionResult<TicketResponseDto>> AssignTicket(int id, [FromBody] int agentId, [FromQuery] int assignedByUserId = 1)
        {
            try
            {
                var updatedTicket = await _ticketService.AssignTicketAsync(id, agentId, assignedByUserId);
                var fullTicket = await _ticketService.GetTicketAsync(updatedTicket.Id);
                
                if (fullTicket == null)
                {
                    return StatusCode(500, new { message = "Failed to retrieve updated ticket" });
                }

                var ticketDto = await MapToTicketResponseDtoAsync(fullTicket);
                return Ok(new
                {
                    ticket = ticketDto,
                    message = $"Ticket assigned to {fullTicket.AssignedAgent?.FullName}",
                    assignmentInfo = new
                    {
                        method = fullTicket.AssignmentMethod,
                        reason = fullTicket.AssignmentReason,
                        assignedAt = fullTicket.AssignedAt
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while assigning the ticket", error = ex.Message });
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<CommentDto>> AddComment(int id, AddCommentDto addCommentDto, [FromQuery] int userId = 1)
        {
            try
            {
                var comment = await _ticketService.AddCommentAsync(
                    id, 
                    userId, 
                    addCommentDto.CommentText, 
                    addCommentDto.IsInternal,
                    addCommentDto.UseAIAssistance);
                
                // Get the comment with user details
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                
                var commentDto = new CommentDto
                {
                    Id = comment.Id,
                    CommentText = comment.CommentText,
                    IsInternal = comment.IsInternal,
                    CreatedAt = comment.CreatedAt,
                    User = MapToUserDto(user!),
                    IsAIGenerated = addCommentDto.UseAIAssistance,
                    AIConfidence = addCommentDto.UseAIAssistance ? 0.82 : null
                };

                return CreatedAtAction(nameof(GetTicket), new { id = id }, new
                {
                    comment = commentDto,
                    message = addCommentDto.UseAIAssistance ? "AI-assisted comment added successfully" : "Comment added successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the comment", error = ex.Message });
            }
        }

        [HttpGet("{id}/suggest-agent")]
        public async Task<ActionResult<UserDto>> SuggestBestAgent(int id)
        {
            try
            {
                var suggestedAgent = await _ticketService.SuggestBestAgentAsync(id);
                if (suggestedAgent == null)
                {
                    return NotFound(new { message = "No suitable agent found" });
                }

                // Get alternative agents
                var availableAgents = await _unitOfWork.Users.GetAvailableAgentsAsync();
                var alternatives = availableAgents
                    .Where(a => a.Id != suggestedAgent.Id)
                    .Take(3)
                    .Select(a => new
                    {
                        a.Id,
                        a.FullName,
                        currentWorkload = a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress),
                        skills = a.AgentSkills.Select(s => s.Skill.Name).ToArray()
                    })
                    .ToList();

                var result = new
                {
                    suggestedAgent = new
                    {
                        suggestedAgent.Id,
                        suggestedAgent.FullName,
                        suggestedAgent.Email,
                        currentWorkload = suggestedAgent.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress),
                        skills = suggestedAgent.AgentSkills.Select(s => new { s.Skill.Name, s.ProficiencyLevel }).ToArray()
                    },
                    reasoning = "Selected based on skills match, current workload, and availability",
                    confidence = 0.85,
                    alternativeAgents = alternatives
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while suggesting an agent", error = ex.Message });
            }
        }

        [HttpPost("{id}/escalate")]
        public async Task<ActionResult> EscalateTicket(int id, [FromBody] EscalateTicketDto escalateDto, [FromQuery] int userId = 1)
        {
            try
            {
                var ticket = await _unitOfWork.Tickets.GetByIdAsync(id);
                if (ticket == null)
                    return NotFound(new { message = "Ticket not found" });

                // Update escalation details
                ticket.IsEscalated = true;
                ticket.EscalationReason = escalateDto.Reason;
                ticket.EscalatedById = userId;
                ticket.EscalatedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;

                // If no agent assigned, try to assign to admin/senior agent
                if (ticket.AssignedAgentId == null)
                {
                    var admins = await _unitOfWork.Users.GetUsersByRoleAsync(UserRole.Admin);
                    if (admins.Any())
                    {
                        var leastBusyAdmin = admins
                            .OrderBy(a => a.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress))
                            .First();
                        
                        ticket.AssignedAgentId = leastBusyAdmin.Id;
                        ticket.AssignmentMethod = "Escalated";
                        ticket.AssignmentReason = "Escalated to admin due to: " + escalateDto.Reason;
                        ticket.Status = TicketStatus.InProgress;
                    }
                }

                // Increase priority if escalated
                if (ticket.Priority < Priority.High)
                {
                    ticket.Priority = Priority.High;
                }

                _unitOfWork.Tickets.Update(ticket);

                // Create history entry
                var historyEntry = new TicketHistory
                {
                    TicketId = id,
                    UserId = userId,
                    Action = "Escalated",
                    NewValue = escalateDto.Reason,
                    Details = JsonSerializer.Serialize(new { 
                        EscalationReason = escalateDto.Reason,
                        NewPriority = ticket.Priority.ToString(),
                        AssignedTo = ticket.AssignedAgentId
                    }),
                    CreatedAt = DateTime.UtcNow
                };
                
                await _unitOfWork.TicketHistory.AddAsync(historyEntry);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    message = "Ticket escalated successfully",
                    escalationDetails = new
                    {
                        reason = escalateDto.Reason,
                        escalatedAt = ticket.EscalatedAt,
                        newPriority = ticket.Priority.ToString(),
                        assignedAgent = ticket.AssignedAgentId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while escalating the ticket", error = ex.Message });
            }
        }

        [HttpGet("{id}/timeline")]
        public async Task<ActionResult> GetTicketTimeline(int id)
        {
            try
            {
                var ticket = await _unitOfWork.Tickets.GetByIdAsync(id);
                if (ticket == null)
                    return NotFound(new { message = "Ticket not found" });

                var history = await _unitOfWork.TicketHistory.FindAsync(h => h.TicketId == id);
                var comments = await _unitOfWork.TicketComments.FindAsync(c => c.TicketId == id);

                var timeline = new List<object>();

                // Add creation event
                timeline.Add(new
                {
                    type = "created",
                    timestamp = ticket.CreatedAt,
                    user = (await _unitOfWork.Users.GetByIdAsync(ticket.CustomerId))?.FullName ?? "Unknown",
                    action = "Ticket Created",
                    details = new { ticket.Title, ticket.Priority, ticket.Status }
                });

                // Add history events
                foreach (var h in history.OrderBy(h => h.CreatedAt))
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(h.UserId);
                    timeline.Add(new
                    {
                        type = h.Action.ToLower(),
                        timestamp = h.CreatedAt,
                        user = user?.FullName ?? "System",
                        action = h.Action,
                        oldValue = h.OldValue,
                        newValue = h.NewValue,
                        details = h.Details
                    });
                }

                // Add comment events
                foreach (var c in comments.OrderBy(c => c.CreatedAt))
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(c.UserId);
                    timeline.Add(new
                    {
                        type = c.IsInternal ? "internal_comment" : "comment",
                        timestamp = c.CreatedAt,
                        user = user?.FullName ?? "Unknown",
                        action = c.IsInternal ? "Internal Comment Added" : "Comment Added",
                        content = c.CommentText.Length > 100 ? c.CommentText.Substring(0, 100) + "..." : c.CommentText,
                        isInternal = c.IsInternal
                    });
                }

                var orderedTimeline = timeline
                    .OrderBy(t => (DateTime)t.GetType().GetProperty("timestamp")!.GetValue(t)!)
                    .ToList();

                return Ok(new
                {
                    ticketId = id,
                    ticketNumber = ticket.TicketNumber,
                    timeline = orderedTimeline,
                    summary = new
                    {
                        totalEvents = orderedTimeline.Count,
                        createdAt = ticket.CreatedAt,
                        lastUpdated = ticket.UpdatedAt,
                        currentStatus = ticket.Status.ToString(),
                        isEscalated = ticket.IsEscalated
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving ticket timeline", error = ex.Message });
            }
        }

        [HttpGet("queue")]
        public async Task<ActionResult> GetUnassignedTickets()
        {
            try
            {
                var unassignedTickets = await _unitOfWork.Tickets.FindAsync(t => 
                    t.AssignedAgentId == null && t.Status == TicketStatus.New);

                var queuedTickets = new List<object>();
                
                foreach (var ticket in unassignedTickets.OrderByDescending(t => t.Priority).ThenBy(t => t.CreatedAt))
                {
                    var customer = await _unitOfWork.Users.GetByIdAsync(ticket.CustomerId);
                    var category = ticket.CategoryId.HasValue 
                        ? await _unitOfWork.Categories.GetByIdAsync(ticket.CategoryId.Value) 
                        : null;

                    queuedTickets.Add(new
                    {
                        ticket.Id,
                        ticket.TicketNumber,
                        ticket.Title,
                        ticket.Priority,
                        ticket.CreatedAt,
                        customer = customer?.FullName ?? "Unknown",
                        category = category?.Name,
                        waitingTime = DateTime.UtcNow - ticket.CreatedAt,
                        isOverdue = ticket.FirstResponseDeadline < DateTime.UtcNow,
                        assignmentMethod = ticket.AssignmentMethod,
                        assignmentReason = ticket.AssignmentReason
                    });
                }

                return Ok(new
                {
                    queuedTickets,
                    summary = new
                    {
                        total = queuedTickets.Count,
                        byPriority = queuedTickets.GroupBy(t => ((Priority)t.GetType().GetProperty("Priority")!.GetValue(t)!).ToString())
                                                  .ToDictionary(g => g.Key, g => g.Count()),
                        overdueCount = queuedTickets.Count(t => (bool)t.GetType().GetProperty("isOverdue")!.GetValue(t)!),
                        avgWaitingTime = queuedTickets.Any() 
                            ? queuedTickets.Average(t => ((TimeSpan)t.GetType().GetProperty("waitingTime")!.GetValue(t)!).TotalHours)
                            : 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving queue", error = ex.Message });
            }
        }

        // Helper mapping methods
        private async Task<TicketResponseDto> MapToTicketResponseDtoAsync(
            Ticket ticket, 
            User? customer = null, 
            User? assignedAgent = null, 
            Category? category = null)
        {
            customer ??= ticket.Customer ?? await _unitOfWork.Users.GetByIdAsync(ticket.CustomerId);
            if (ticket.AssignedAgentId.HasValue)
            {
                assignedAgent ??= ticket.AssignedAgent ?? await _unitOfWork.Users.GetByIdAsync(ticket.AssignedAgentId.Value);
            }
            if (ticket.CategoryId.HasValue)
            {
                category ??= ticket.Category ?? await _unitOfWork.Categories.GetByIdAsync(ticket.CategoryId.Value);
            }

            // Parse AI analysis if available
            AIAnalysisDto? aiAnalysis = null;
            if (!string.IsNullOrEmpty(ticket.AIAnalysis))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<JsonElement>(ticket.AIAnalysis);
                    aiAnalysis = new AIAnalysisDto
                    {
                        SuggestedCategory = parsed.TryGetProperty("suggestedCategory", out var cat) ? cat.GetString() ?? "" : "",
                        SuggestedPriority = parsed.TryGetProperty("suggestedPriority", out var pri) ? (Priority)pri.GetInt32() : Priority.Medium,
                        SentimentScore = parsed.TryGetProperty("sentimentScore", out var sent) ? sent.GetDouble() : 0.5,
                        SentimentLabel = parsed.TryGetProperty("sentimentScore", out var sentScore) 
                            ? sentScore.GetDouble() switch
                            {
                                < 0.3 => "Negative",
                                < 0.7 => "Neutral",
                                _ => "Positive"
                            } : "Neutral",
                        Confidence = parsed.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.0,
                        AnalyzedAt = parsed.TryGetProperty("analyzedAt", out var date) ? date.GetDateTime() : DateTime.MinValue,
                        KeyWords = parsed.TryGetProperty("keyWords", out var kw) ? kw.GetString() : null,
                        UrgencyIndicators = parsed.TryGetProperty("urgencyIndicators", out var ui) ? ui.GetString() : null
                    };
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            // Parse business impact if available
            BusinessImpactDto? businessImpact = null;
            if (!string.IsNullOrEmpty(ticket.BusinessImpactData))
            {
                try
                {
                    businessImpact = JsonSerializer.Deserialize<BusinessImpactDto>(ticket.BusinessImpactData);
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            return new TicketResponseDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt,
                DueDate = ticket.DueDate,
                Customer = customer != null ? MapToUserDto(customer) : new UserDto { Id = ticket.CustomerId, FullName = "Unknown User" },
                AssignedAgent = assignedAgent != null ? MapToUserDto(assignedAgent) : null,
                Category = category != null ? MapToCategoryDto(category) : null,
                Comments = ticket.Comments?.Select(MapToCommentDto).ToList() ?? new List<CommentDto>(),
                Attachments = ticket.Attachments?.Select(MapToAttachmentDto).ToList() ?? new List<AttachmentDto>(),
                AIAnalysis = aiAnalysis,
                BusinessImpact = businessImpact,
                Assignment = new TicketAssignmentDto
                {
                    AssignmentMethod = ticket.AssignmentMethod,
                    AssignmentReason = ticket.AssignmentReason,
                    AssignedAt = ticket.AssignedAt,
                    IsEscalated = ticket.IsEscalated,
                    EscalationReason = ticket.EscalationReason
                }
            };
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        private static CategoryDto MapToCategoryDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name ?? string.Empty,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                Level = category.Level,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }

        private static CommentDto MapToCommentDto(TicketComment comment)
        {
            return new CommentDto
            {
                Id = comment.Id,
                CommentText = comment.CommentText ?? string.Empty,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt,
                User = comment.User != null ? MapToUserDto(comment.User) : new UserDto { Id = comment.UserId, FullName = "Unknown" }
            };
        }

        private static AttachmentDto MapToAttachmentDto(Attachment attachment)
        {
            return new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName ?? string.Empty,
                OriginalFileName = attachment.OriginalFileName ?? string.Empty,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType ?? string.Empty,
                CreatedAt = attachment.CreatedAt,
                UploadedBy = attachment.UploadedBy != null ? MapToUserDto(attachment.UploadedBy) : new UserDto { Id = attachment.UploadedById, FullName = "Unknown" }
            };
        }
    }

    // Additional DTOs for new endpoints
    public class EscalateTicketDto
    {
        public string Reason { get; set; } = string.Empty;
        public bool NotifyManagement { get; set; } = true;
    }
}