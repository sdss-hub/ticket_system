using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class TicketHistory
    {
        public int Id { get; set; }
        
        public int TicketId { get; set; }
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; 
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        
        public string? Details { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual Ticket Ticket { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}