using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class Skill
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<AgentSkill> AgentSkills { get; set; } = new List<AgentSkill>();
    }
}