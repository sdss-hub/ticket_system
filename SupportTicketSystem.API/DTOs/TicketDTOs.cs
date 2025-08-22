using System.ComponentModel.DataAnnotations;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.API.DTOs
{
    public class CreateTicketDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? CategoryId { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public DateTime? DueDate { get; set; }
    }

    public class UpdateTicketDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public Priority? Priority { get; set; }
        public TicketStatus? Status { get; set; }
        public int? AssignedAgentId { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public UserDto Customer { get; set; } = null!;
        public UserDto? AssignedAgent { get; set; }
        public CategoryDto? Category { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public UserDto UploadedBy { get; set; } = null!;
    }

    public class AddCommentDto
    {
        [Required]
        public string CommentText { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;
    }
}
