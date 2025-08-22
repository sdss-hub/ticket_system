using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
    {
        public void Configure(EntityTypeBuilder<TicketComment> builder)
        {
            builder.ToTable("TicketComments");
            
            builder.HasKey(tc => tc.Id);
            
            builder.Property(tc => tc.CommentText)
                .IsRequired();
            
            builder.HasIndex(tc => tc.TicketId);
            builder.HasIndex(tc => tc.UserId);
            builder.HasIndex(tc => tc.CreatedAt);
            
            builder.HasMany(tc => tc.Attachments)
                .WithOne(a => a.Comment)
                .HasForeignKey(a => a.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
