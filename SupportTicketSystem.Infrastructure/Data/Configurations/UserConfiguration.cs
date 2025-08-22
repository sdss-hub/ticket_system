using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Enums;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(u => u.Role)
                .HasConversion<int>();
            
            builder.Property(u => u.ProfileSettings)
                .HasColumnType("JSON");
            
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Role);
            
            builder.HasMany(u => u.CreatedTickets)
                .WithOne(t => t.Customer)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(u => u.AssignedTickets)
                .WithOne(t => t.AssignedAgent)
                .HasForeignKey(t => t.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);
            
            builder.HasMany(u => u.Comments)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(u => u.Attachments)
                .WithOne(a => a.UploadedBy)
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(u => u.HistoryEntries)
                .WithOne(h => h.User)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Ignore(u => u.FullName);
        }
    }
}
