using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.API.DTOs;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Interfaces;

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
            [FromQuery] string? search = null)
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
                                               t.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

                var result = new List<TicketResponseDto>();
                
                foreach (var ticket in tickets.Take(50)) // Limit to 50 for performance
                {
                    // Load related entities manually if needed
                    var customer = await _unitOfWork.Users.GetByIdAsync(ticket.CustomerId);
                    var assignedAgent = ticket.AssignedAgentId.HasValue 
                        ? await _unitOfWork.Users.GetByIdAsync(ticket.AssignedAgentId.Value) 
                        : null;
                    var category = ticket.CategoryId.HasValue 
                        ? await _unitOfWork.Categories.GetByIdAsync(ticket.CategoryId.Value) 
                        : null;

                    var ticketDto = new TicketResponseDto
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
                        Comments = new List<CommentDto>(),
                        Attachments = new List<AttachmentDto>()
                    };
                    
                    result.Add(ticketDto);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving tickets", error = ex.Message, stackTrace = ex.StackTrace });
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

                var ticketDto = MapToTicketResponseDto(ticket);
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
                var ticket = new Ticket
                {
                    Title = createTicketDto.Title,
                    Description = createTicketDto.Description,
                    CustomerId = createTicketDto.CustomerId,
                    CategoryId = createTicketDto.CategoryId,
                    Priority = createTicketDto.Priority,
                    DueDate = createTicketDto.DueDate,
                    Status = TicketStatus.New
                };

                var createdTicket = await _ticketService.CreateTicketAsync(ticket);
                var fullTicket = await _ticketService.GetTicketAsync(createdTicket.Id);
                
                if (fullTicket == null)
                {
                    return StatusCode(500, new { message = "Failed to retrieve created ticket" });
                }

                var ticketDto = MapToTicketResponseDto(fullTicket);
                return CreatedAtAction(nameof(GetTicket), new { id = ticketDto.Id }, ticketDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the ticket", error = ex.Message });
            }
        }

        // Helper mapping methods with null safety
        private static TicketResponseDto MapToTicketResponseDto(Ticket ticket)
        {
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
                Customer = ticket.Customer != null ? MapToUserDto(ticket.Customer) : new UserDto { Id = ticket.CustomerId, FullName = "Unknown" },
                AssignedAgent = ticket.AssignedAgent != null ? MapToUserDto(ticket.AssignedAgent) : null,
                Category = ticket.Category != null ? MapToCategoryDto(ticket.Category) : null,
                Comments = ticket.Comments?.Select(MapToCommentDto).ToList() ?? new List<CommentDto>(),
                Attachments = ticket.Attachments?.Select(MapToAttachmentDto).ToList() ?? new List<AttachmentDto>()
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
}
