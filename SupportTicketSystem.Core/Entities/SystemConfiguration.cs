using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class SystemConfiguration
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ConfigKey { get; set; } = string.Empty;
        
        [Required]
        public string ConfigValue { get; set; } = string.Empty; 
        public string? Description { get; set; }
        
        public int UpdatedById { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual User UpdatedBy { get; set; } = null!;
    }
}
