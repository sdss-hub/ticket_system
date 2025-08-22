using Microsoft.AspNetCore.Mvc;
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
                IEnumerable<Ticket> tickets;

                if (!string.IsNullOrEmpty(search))
                {
                    tickets = await _ticketService.SearchTicketsAsync(search);
                }
                else if (status.HasValue)
                {
                    tickets = await _unitOfWork.Tickets.GetTicketsByStatusAsync(status.Value);
                }
                else if (customerId.HasValue)
                {
                    tickets = await _unitOfWork.Tickets.GetTicketsByCustomerAsync(customerId.Value);
                }
                else if (agentId.HasValue)
                {
                    tickets = await _unitOfWork.Tickets.GetTicketsByAgentAsync(agentId.Value);
                }
                else
                {
                    tickets = await _unitOfWork.Tickets.GetAllAsync();
                }

                var ticketDtos = tickets.Select(MapToTicketResponseDto).ToList();
                return Ok(ticketDtos);
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

                var ticketDto = MapToTicketResponseDto(fullTicket);
                return Ok(ticketDto);
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

                var ticketDto = MapToTicketResponseDto(fullTicket);
                return Ok(ticketDto);
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
                var comment = await _ticketService.AddCommentAsync(id, userId, addCommentDto.CommentText, addCommentDto.IsInternal);
                
                // Get the comment with user details
                var commentWithUser = await _unitOfWork.TicketComments.FirstOrDefaultAsync(c => c.Id == comment.Id);
                if (commentWithUser?.User == null)
                {
                    commentWithUser = comment;
                    commentWithUser.User = await _unitOfWork.Users.GetByIdAsync(userId) ?? new User();
                }

                var commentDto = MapToCommentDto(commentWithUser);
                return CreatedAtAction(nameof(GetTicket), new { id = id }, commentDto);
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

                var agentDto = MapToUserDto(suggestedAgent);
                return Ok(agentDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while suggesting an agent", error = ex.Message });
            }
        }

        // Helper mapping methods
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
                Customer = MapToUserDto(ticket.Customer),
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
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
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
                Name = category.Name,
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
                CommentText = comment.CommentText,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt,
                User = MapToUserDto(comment.User)
            };
        }

        private static AttachmentDto MapToAttachmentDto(Attachment attachment)
        {
            return new AttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                OriginalFileName = attachment.OriginalFileName,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType,
                CreatedAt = attachment.CreatedAt,
                UploadedBy = MapToUserDto(attachment.UploadedBy)
            };
        }
    }
}
