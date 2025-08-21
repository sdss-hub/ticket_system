using System.ComponentModel.DataAnnotations;

namespace SupportTicketSystem.Core.Entities
{
    public class AgentSkill
    {
        public int AgentId { get; set; }
        public int SkillId { get; set; }
        
        [Range(1, 5)]
        public int ProficiencyLevel { get; set; } = 1;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual User Agent { get; set; } = null!;
        public virtual Skill Skill { get; set; } = null!;
    }
}