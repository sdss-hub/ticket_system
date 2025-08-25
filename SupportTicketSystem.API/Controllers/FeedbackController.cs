using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.API.DTOs;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult> CreateFeedback([FromBody] CreateFeedbackDto createFeedbackDto)
        {
            try
            {
                // Validate ticket exists
                var ticket = await _context.Tickets.FindAsync(createFeedbackDto.TicketId);
                if (ticket == null)
                {
                    return NotFound(new { message = $"Ticket with ID {createFeedbackDto.TicketId} not found" });
                }

                // Validate customer exists
                var customer = await _context.Users.FindAsync(createFeedbackDto.CustomerId);
                if (customer == null)
                {
                    return NotFound(new { message = $"Customer with ID {createFeedbackDto.CustomerId} not found" });
                }

                if (customer.Role != UserRole.Customer)
                {
                    return BadRequest(new { message = "Only customers can provide feedback" });
                }

                // Check if customer owns the ticket
                if (ticket.CustomerId != createFeedbackDto.CustomerId)
                {
                    return Forbid("You can only provide feedback for your own tickets");
                }

                // Check if feedback already exists for this ticket
                var existingFeedback = await _context.Feedback.FirstOrDefaultAsync(f => f.TicketId == createFeedbackDto.TicketId);
                if (existingFeedback != null)
                {
                    return Conflict(new { message = "Feedback already exists for this ticket" });
                }

                // Create feedback
                var feedback = new Feedback
                {
                    TicketId = createFeedbackDto.TicketId,
                    CustomerId = createFeedbackDto.CustomerId,
                    Rating = createFeedbackDto.Rating,
                    Comment = createFeedbackDto.Comment,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Feedback.Add(feedback);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Feedback created successfully",
                    data = new {
                        id = feedback.Id,
                        ticketId = feedback.TicketId,
                        rating = feedback.Rating,
                        comment = feedback.Comment,
                        createdAt = feedback.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = "An error occurred while creating feedback", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("ticket/{ticketId}")]
        public async Task<ActionResult> GetFeedbackByTicket(int ticketId)
        {
            try
            {
                var feedback = await _context.Feedback
                    .FirstOrDefaultAsync(f => f.TicketId == ticketId);
                
                if (feedback == null)
                {
                    return Ok(new { success = true, data = (object?)null });
                }

                return Ok(new { 
                    success = true, 
                    data = new {
                        id = feedback.Id,
                        ticketId = feedback.TicketId,
                        customerId = feedback.CustomerId,
                        rating = feedback.Rating,
                        comment = feedback.Comment,
                        createdAt = feedback.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = "An error occurred while retrieving feedback", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetAllFeedback()
        {
            try
            {
                var feedbacks = await _context.Feedback
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new {
                        id = f.Id,
                        ticketId = f.TicketId,
                        customerId = f.CustomerId,
                        rating = f.Rating,
                        comment = f.Comment,
                        createdAt = f.CreatedAt
                    })
                    .ToListAsync();
                
                return Ok(new { success = true, data = feedbacks });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = "An error occurred while retrieving feedback", 
                    error = ex.Message 
                });
            }
        }
    }
}
