using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.API.DTOs
{
    public class CreateFeedbackDto
    {
        [Required]
        public int TicketId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string Comment { get; set; } = string.Empty;
    }

    public class FeedbackDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int CustomerId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        // Additional information
        public UserDto? Customer { get; set; }
        public TicketBasicDto? Ticket { get; set; }
    }

    public class TicketBasicDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public UserDto? AssignedAgent { get; set; }
    }
}
