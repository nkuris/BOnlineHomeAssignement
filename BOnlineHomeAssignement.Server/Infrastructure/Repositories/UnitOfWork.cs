using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using DomainProgram = BOnlineHomeAssignement.Server.Domain.Entities.Program;

namespace BOnlineHomeAssignement.Server.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation managing multiple repositories
    /// and coordinating transactions across the application.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        private IRepository<Patient>? _patientRepository;
        private IRepository<Lead>? _leadRepository;
        private IRepository<DomainProgram>? _programRepository;
        private IRepository<Enrollment>? _enrollmentRepository;
        private IRepository<TaskItem>? _taskItemRepository;
        private IRepository<FollowUpRule>? _followUpRuleRepository;
        private IRepository<Tenant>? _tenantRepository;
        private IRepository<TenantConfiguration>? _tenantConfigurationRepository;
        private IRepository<AuditLog>? _auditLogRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<Patient> Patients =>
            _patientRepository ??= new Repository<Patient>(_context);

        public IRepository<Lead> Leads =>
            _leadRepository ??= new Repository<Lead>(_context);

        public IRepository<DomainProgram> Programs =>
            _programRepository ??= new Repository<DomainProgram>(_context);

        public IRepository<Enrollment> Enrollments =>
            _enrollmentRepository ??= new Repository<Enrollment>(_context);

        public IRepository<TaskItem> TaskItems =>
            _taskItemRepository ??= new Repository<TaskItem>(_context);

        public IRepository<FollowUpRule> FollowUpRules =>
            _followUpRuleRepository ??= new Repository<FollowUpRule>(_context);

        public IRepository<Tenant> Tenants =>
            _tenantRepository ??= new Repository<Tenant>(_context);

        public IRepository<TenantConfiguration> TenantConfigurations =>
            _tenantConfigurationRepository ??= new Repository<TenantConfiguration>(_context);

        public IRepository<AuditLog> AuditLogs =>
            _auditLogRepository ??= new Repository<AuditLog>(_context);

        /// <summary>Save all changes from all repositories in a single transaction.</summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>Begin a database transaction.</summary>
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        /// <summary>Commit the current transaction.</summary>
        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                await _transaction?.CommitAsync()!;
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        /// <summary>Rollback the current transaction.</summary>
        public async Task RollbackTransactionAsync()
        {
            try
            {
                await _transaction?.RollbackAsync()!;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
