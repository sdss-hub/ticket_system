using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
    {
        public void Configure(EntityTypeBuilder<TicketHistory> builder)
        {
            builder.ToTable("TicketHistory");
            
            builder.HasKey(th => th.Id);
            
            builder.Property(th => th.Action)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(th => th.OldValue)
                .HasMaxLength(1000);
            
            builder.Property(th => th.NewValue)
                .HasMaxLength(1000);
            
            builder.Property(th => th.Details)
                .HasColumnType("JSON");
            
            builder.HasIndex(th => th.TicketId);
            builder.HasIndex(th => th.UserId);
            builder.HasIndex(th => th.Action);
            builder.HasIndex(th => th.CreatedAt);
        }
    }
}
