using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class AgentSkillConfiguration : IEntityTypeConfiguration<AgentSkill>
    {
        public void Configure(EntityTypeBuilder<AgentSkill> builder)
        {
            builder.ToTable("AgentSkills", t => 
                t.HasCheckConstraint("CK_AgentSkill_ProficiencyLevel", "ProficiencyLevel >= 1 AND ProficiencyLevel <= 5"));
            
            builder.HasKey(as_ => new { as_.AgentId, as_.SkillId });
            
            builder.Property(as_ => as_.ProficiencyLevel)
                .HasDefaultValue(1);
            
            builder.HasOne(as_ => as_.Agent)
                .WithMany(u => u.AgentSkills)
                .HasForeignKey(as_ => as_.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(as_ => as_.Skill)
                .WithMany(s => s.AgentSkills)
                .HasForeignKey(as_ => as_.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasIndex(as_ => as_.AgentId);
            builder.HasIndex(as_ => as_.SkillId);
        }
    }
}
