using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("Attachments");
            
            builder.HasKey(a => a.Id);
            
            builder.Property(a => a.FileName)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(a => a.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(a => a.MimeType)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(a => a.FilePath)
                .HasMaxLength(500);
            
            builder.Property(a => a.FileData)
                .HasColumnType("BLOB");
            
            builder.HasIndex(a => a.TicketId);
            builder.HasIndex(a => a.CommentId);
            builder.HasIndex(a => a.UploadedById);
            builder.HasIndex(a => a.CreatedAt);
        }
    }
}
