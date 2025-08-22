using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class SkillConfiguration : IEntityTypeConfiguration<Skill>
    {
        public void Configure(EntityTypeBuilder<Skill> builder)
        {
            builder.ToTable("Skills");
            
            builder.HasKey(s => s.Id);
            
            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(s => s.Description)
                .HasMaxLength(500);
            
            builder.HasIndex(s => s.Name).IsUnique();
        }
    }
}
