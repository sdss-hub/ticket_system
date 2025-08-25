using Microsoft.EntityFrameworkCore;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Infrastructure.Data.Configurations;

namespace SupportTicketSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TicketTag> TicketTags { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<AgentSkill> AgentSkills { get; set; }
        public DbSet<TicketHistory> TicketHistory { get; set; }
        public DbSet<AIInsight> AIInsights { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<Feedback> Feedback { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new TicketConfiguration());
            modelBuilder.ApplyConfiguration(new TicketCommentConfiguration());
            modelBuilder.ApplyConfiguration(new AttachmentConfiguration());
            modelBuilder.ApplyConfiguration(new TagConfiguration());
            modelBuilder.ApplyConfiguration(new TicketTagConfiguration());
            modelBuilder.ApplyConfiguration(new SkillConfiguration());
            modelBuilder.ApplyConfiguration(new AgentSkillConfiguration());
            modelBuilder.ApplyConfiguration(new TicketHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new AIInsightConfiguration());
            modelBuilder.ApplyConfiguration(new SystemConfigurationConfiguration());
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is User && (e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                ((User)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            var ticketEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is Ticket && (e.State == EntityState.Modified));

            foreach (var entry in ticketEntries)
            {
                ((Ticket)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
