using Microsoft.EntityFrameworkCore.Storage;
using SupportTicketSystem.Core.Entities;
using SupportTicketSystem.Core.Interfaces;
using SupportTicketSystem.Infrastructure.Data;

namespace SupportTicketSystem.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repository instances
        private ITicketRepository? _tickets;
        private IUserRepository? _users;
        private ICategoryRepository? _categories;
        private IBaseRepository<TicketComment>? _ticketComments;
        private IBaseRepository<Attachment>? _attachments;
        private IBaseRepository<Tag>? _tags;
        private IBaseRepository<TicketTag>? _ticketTags;
        private IBaseRepository<Skill>? _skills;
        private IBaseRepository<AgentSkill>? _agentSkills;
        private IBaseRepository<TicketHistory>? _ticketHistory;
        private IBaseRepository<AIInsight>? _aiInsights;
        private IBaseRepository<SystemConfiguration>? _systemConfigurations;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lazy initialization of repositories
        public ITicketRepository Tickets =>
            _tickets ??= new TicketRepository(_context);

        public IUserRepository Users =>
            _users ??= new UserRepository(_context);

        public ICategoryRepository Categories =>
            _categories ??= new CategoryRepository(_context);

        public IBaseRepository<TicketComment> TicketComments =>
            _ticketComments ??= new BaseRepository<TicketComment>(_context);

        public IBaseRepository<Attachment> Attachments =>
            _attachments ??= new BaseRepository<Attachment>(_context);

        public IBaseRepository<Tag> Tags =>
            _tags ??= new BaseRepository<Tag>(_context);

        public IBaseRepository<TicketTag> TicketTags =>
            _ticketTags ??= new BaseRepository<TicketTag>(_context);

        public IBaseRepository<Skill> Skills =>
            _skills ??= new BaseRepository<Skill>(_context);

        public IBaseRepository<AgentSkill> AgentSkills =>
            _agentSkills ??= new BaseRepository<AgentSkill>(_context);

        public IBaseRepository<TicketHistory> TicketHistory =>
            _ticketHistory ??= new BaseRepository<TicketHistory>(_context);

        public IBaseRepository<AIInsight> AIInsights =>
            _aiInsights ??= new BaseRepository<AIInsight>(_context);

        public IBaseRepository<SystemConfiguration> SystemConfigurations =>
            _systemConfigurations ??= new BaseRepository<SystemConfiguration>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
