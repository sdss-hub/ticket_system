using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");
            
            builder.HasKey(t => t.Id);
            
            builder.Property(t => t.TicketNumber)
                .IsRequired()
                .HasMaxLength(20);
            
            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(t => t.Description)
                .IsRequired();
            
            builder.Property(t => t.Priority)
                .HasConversion<int>();
            
            builder.Property(t => t.Status)
                .HasConversion<int>();
            
            builder.Property(t => t.AIAnalysis)
                .HasColumnType("JSON");
            
            // Indexes
            builder.HasIndex(t => t.TicketNumber).IsUnique();
            builder.HasIndex(t => t.CustomerId);
            builder.HasIndex(t => t.AssignedAgentId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.Priority);
            builder.HasIndex(t => t.CreatedAt);
            
            // Relationships
            builder.HasMany(t => t.Comments)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.Attachments)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.History)
                .WithOne(h => h.Ticket)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.AIInsights)
                .WithOne(ai => ai.Ticket)
                .HasForeignKey(ai => ai.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
