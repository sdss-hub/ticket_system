using SupportTicketSystem.Core.Entities;

namespace SupportTicketSystem.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ITicketRepository Tickets { get; }
        IUserRepository Users { get; }
        ICategoryRepository Categories { get; }
        IBaseRepository<TicketComment> TicketComments { get; }
        IBaseRepository<Attachment> Attachments { get; }
        IBaseRepository<Tag> Tags { get; }
        IBaseRepository<TicketTag> TicketTags { get; }
        IBaseRepository<Skill> Skills { get; }
        IBaseRepository<AgentSkill> AgentSkills { get; }
        IBaseRepository<TicketHistory> TicketHistory { get; }
        IBaseRepository<AIInsight> AIInsights { get; }
        IBaseRepository<SystemConfiguration> SystemConfigurations { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
