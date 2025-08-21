using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class TicketComment
    {
        public int Id { get; set; }
        
        public int TicketId { get; set; }
        public int UserId { get; set; }
        
        [Required]
        public string CommentText { get; set; } = string.Empty;
        
        public bool IsInternal { get; set; } = false; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual Ticket Ticket { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}