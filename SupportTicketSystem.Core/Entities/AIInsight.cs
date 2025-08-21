using System.ComponentModel.DataAnnotations;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.Core.Entities
{
    public class AIInsight
    {
        public int Id { get; set; }
        
        public int TicketId { get; set; }
        
        public InsightType InsightType { get; set; }
        
        [Range(0.0, 1.0)]
        public double Confidence { get; set; }
        
        [Required]
        public string Data { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual Ticket Ticket { get; set; } = null!;
    }
}