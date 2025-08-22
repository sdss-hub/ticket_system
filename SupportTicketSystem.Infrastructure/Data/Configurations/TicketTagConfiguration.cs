using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class TicketTagConfiguration : IEntityTypeConfiguration<TicketTag>
    {
        public void Configure(EntityTypeBuilder<TicketTag> builder)
        {
            builder.ToTable("TicketTags");
            
            builder.HasKey(tt => new { tt.TicketId, tt.TagId });
            
            builder.HasOne(tt => tt.Ticket)
                .WithMany(t => t.TicketTags)
                .HasForeignKey(tt => tt.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(tt => tt.Tag)
                .WithMany(t => t.TicketTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasIndex(tt => tt.TicketId);
            builder.HasIndex(tt => tt.TagId);
        }
    }
}
