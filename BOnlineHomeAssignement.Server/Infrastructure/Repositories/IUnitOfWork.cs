using BOnlineHomeAssignement.Server.Domain.Entities;
using DomainProgram = BOnlineHomeAssignement.Server.Domain.Entities.Program;

namespace BOnlineHomeAssignement.Server.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work pattern interface for managing multiple repositories
    /// and coordinating transactions across multiple entities.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Patient> Patients { get; }
        IRepository<Lead> Leads { get; }
        IRepository<DomainProgram> Programs { get; }
        IRepository<Enrollment> Enrollments { get; }
        IRepository<TaskItem> TaskItems { get; }
        IRepository<FollowUpRule> FollowUpRules { get; }
        IRepository<Tenant> Tenants { get; }
        IRepository<TenantConfiguration> TenantConfigurations { get; }
        IRepository<AuditLog> AuditLogs { get; }

        /// <summary>Save all changes from all repositories in a single transaction.</summary>
        Task<int> SaveChangesAsync();

        /// <summary>Begin a database transaction.</summary>
        Task BeginTransactionAsync();

        /// <summary>Commit the current transaction.</summary>
        Task CommitTransactionAsync();

        /// <summary>Rollback the current transaction.</summary>
        Task RollbackTransactionAsync();
    }
}
