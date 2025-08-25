using System.ComponentModel.DataAnnotations;
using SupportTicketSystem.Core.Enums;
using SupportTicketSystem.Core.Entities;

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

        // Business impact questions (instead of direct priority selection)
        public BusinessImpactDto? BusinessImpact { get; set; }

        public DateTime? PreferredResolutionDate { get; set; }
    }

    public class BusinessImpactDto
    {
        // Is this blocking work?
        public BlockingLevel BlockingLevel { get; set; } = BlockingLevel.NotBlocking;

        // How many people affected?
        public ImpactScope ImpactScope { get; set; } = ImpactScope.Individual;

        // Any urgent deadline?
        public DateTime? UrgentDeadline { get; set; }

        // Customer's perceived urgency (optional context)
        public string? AdditionalContext { get; set; }

        // Convert to Core entity
        public BusinessImpact ToBusinessImpact()
        {
            return new BusinessImpact
            {
                BlockingLevel = BlockingLevel,
                ImpactScope = ImpactScope,
                UrgentDeadline = UrgentDeadline,
                AdditionalContext = AdditionalContext
            };
        }
    }

    public class UpdateTicketDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        
        // Only agents/admins can update priority and status
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

        // AI analysis results
        public AIAnalysisDto? AIAnalysis { get; set; }
        
        // Business impact information
        public BusinessImpactDto? BusinessImpact { get; set; }
        
        // Assignment information
        public TicketAssignmentDto? Assignment { get; set; }
        
        // Customer feedback
        public FeedbackDto? Feedback { get; set; }
    }

    public class AIAnalysisDto
    {
        public string SuggestedCategory { get; set; } = string.Empty;
        public Priority SuggestedPriority { get; set; }
        public double SentimentScore { get; set; }
        public string SentimentLabel { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public string? KeyWords { get; set; }
        public string? UrgencyIndicators { get; set; }
    }

    public class TicketAssignmentDto
    {
        public string AssignmentMethod { get; set; } = string.Empty;
        public string? AssignmentReason { get; set; }
        public UserDto[]? AlternativeAgents { get; set; }
        public DateTime? AssignedAt { get; set; }
        public bool IsEscalated { get; set; }
        public string? EscalationReason { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto User { get; set; } = null!;
        public bool IsAIGenerated { get; set; } = false;
        public double? AIConfidence { get; set; }
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
        
        // AI assistance options
        public bool UseAIAssistance { get; set; } = false;
        public string? AIContext { get; set; }
    }

    public class EscalateTicketDto
    {
        public string Reason { get; set; } = string.Empty;
        public bool NotifyManagement { get; set; } = true;
    }
}
