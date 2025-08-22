using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Infrastructure.Data.Configurations
{
    public class AIInsightConfiguration : IEntityTypeConfiguration<AIInsight>
    {
        public void Configure(EntityTypeBuilder<AIInsight> builder)
        {
            builder.ToTable("AIInsights", t => 
                t.HasCheckConstraint("CK_AIInsight_Confidence", "Confidence >= 0.0 AND Confidence <= 1.0"));
            
            builder.HasKey(ai => ai.Id);
            
            builder.Property(ai => ai.InsightType)
                .HasConversion<int>();
            
            builder.Property(ai => ai.Confidence)
                .HasPrecision(3, 2); 
            
            builder.Property(ai => ai.Data)
                .IsRequired()
                .HasColumnType("JSON");
            
            builder.HasIndex(ai => ai.TicketId);
            builder.HasIndex(ai => ai.InsightType);
            builder.HasIndex(ai => ai.CreatedAt);
        }
    }
}
