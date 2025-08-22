using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
    {
        public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
        {
            builder.ToTable("SystemConfigurations");
            
            builder.HasKey(sc => sc.Id);
            
            builder.Property(sc => sc.ConfigKey)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(sc => sc.ConfigValue)
                .IsRequired()
                .HasColumnType("JSON");
            
            builder.Property(sc => sc.Description)
                .HasMaxLength(500);
            
            builder.HasOne(sc => sc.UpdatedBy)
                .WithMany()
                .HasForeignKey(sc => sc.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasIndex(sc => sc.ConfigKey).IsUnique();
            builder.HasIndex(sc => sc.UpdatedById);
            builder.HasIndex(sc => sc.UpdatedAt);
        }
    }
}
