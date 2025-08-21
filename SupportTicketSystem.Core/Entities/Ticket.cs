using System.ComponentModel.DataAnnotations;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.Core.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;
        
        public int CustomerId { get; set; }
        public int? AssignedAgentId { get; set; }
        public int? CategoryId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public Priority Priority { get; set; } = Priority.Medium;
        public TicketStatus Status { get; set; } = TicketStatus.New;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? DueDate { get; set; }
        
        public string? AIAnalysis { get; set; }
        
        public virtual User Customer { get; set; } = null!;
        public virtual User? AssignedAgent { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public virtual ICollection<TicketTag> TicketTags { get; set; } = new List<TicketTag>();
        public virtual ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
        public virtual ICollection<AIInsight> AIInsights { get; set; } = new List<AIInsight>();
    }
}