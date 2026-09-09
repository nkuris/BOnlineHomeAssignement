using Microsoft.EntityFrameworkCore;
using BOnlineHomeAssignement.Server.Domain.Entities;
using DomainProgram = BOnlineHomeAssignement.Server.Domain.Entities.Program;

namespace BOnlineHomeAssignement.Server.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSets for domain entities
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<FollowUpRule> FollowUpRules { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<DomainProgram> Programs { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantConfiguration> TenantConfigurations { get; set; }

        // DbSets for configurable workflow entities (Phase 3.5)
        public DbSet<ProgramConfiguration> ProgramConfigurations { get; set; }
        public DbSet<ProcessStage> ProcessStages { get; set; }
        public DbSet<PatientPathway> PatientPathways { get; set; }
        public DbSet<FormDefinition> FormDefinitions { get; set; }
        public DbSet<TaskDefinition> TaskDefinitions { get; set; }
        public DbSet<ContentTemplate> ContentTemplates { get; set; }
        public DbSet<BusinessRule> BusinessRules { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        // DbSets for Phase 2 extended entities (Timeline, Events, Triggers, Questionnaires, Communication)
        public DbSet<WorkflowEvent> WorkflowEvents { get; set; }
        public DbSet<Questionnaire> Questionnaires { get; set; }
        public DbSet<QuestionnaireResponse> QuestionnaireResponses { get; set; }
        public DbSet<TriggerExecution> TriggerExecutions { get; set; }
        public DbSet<CommunicationMessage> CommunicationMessages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                base.OnConfiguring(optionsBuilder);
            }
            // Suppress pending model changes warning in container environments where migrations are managed separately
            optionsBuilder?.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Patient
            modelBuilder.Entity<Patient>(eb =>
            {
                eb.HasKey(p => p.Id);
                eb.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
                eb.Property(p => p.LastName).IsRequired().HasMaxLength(100);
                eb.Property(p => p.Email).HasMaxLength(200);
                eb.HasIndex(p => p.Email).HasName("IX_Patient_Email");
                eb.HasIndex(p => new { p.LastName, p.FirstName }).HasName("IX_Patient_Name");
                eb.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Appointment
            modelBuilder.Entity<Appointment>(eb =>
            {
                eb.HasKey(a => a.Id);
                eb.Property(a => a.Title).IsRequired().HasMaxLength(200);
                eb.Property(a => a.Status).IsRequired().HasMaxLength(50);
                eb.Property(a => a.AppointmentType).HasMaxLength(50);
                eb.Property(a => a.Location).HasMaxLength(500);
                eb.Property(a => a.ProviderId).HasMaxLength(200);
                eb.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                eb.Property(a => a.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Foreign key relationship to Patient (NO ACTION to avoid cascade cycles with Enrollment)
                eb.HasOne(a => a.Patient)
                  .WithMany()
                  .HasForeignKey(a => a.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);

                // Foreign key relationship to Enrollment (optional)
                eb.HasOne(a => a.Enrollment)
                  .WithMany()
                  .HasForeignKey(a => a.EnrollmentId)
                  .OnDelete(DeleteBehavior.SetNull);

                // Indexes for common queries
                eb.HasIndex(a => a.PatientId).HasName("IX_Appointment_PatientId");
                eb.HasIndex(a => a.EnrollmentId).HasName("IX_Appointment_EnrollmentId");
                eb.HasIndex(a => a.ScheduledStart).HasName("IX_Appointment_ScheduledStart");
                eb.HasIndex(a => a.Status).HasName("IX_Appointment_Status");
                eb.HasIndex(a => new { a.TenantId, a.PatientId }).HasName("IX_Appointment_Tenant_Patient");
                eb.HasIndex(a => new { a.TenantId, a.EnrollmentId }).HasName("IX_Appointment_Tenant_Enrollment");
            });

            // Lead
            modelBuilder.Entity<Lead>(eb =>
            {
                eb.HasKey(l => l.Id);
                eb.Property(l => l.TenantId).IsRequired();
                eb.Property(l => l.Name).IsRequired().HasMaxLength(200);
                eb.Property(l => l.Email).HasMaxLength(200);
                eb.Property(l => l.Phone).HasMaxLength(50);
                eb.Property(l => l.LeadSource).IsRequired();
                eb.HasIndex(l => l.Email).HasName("IX_Lead_Email");
                eb.HasIndex(l => new { l.TenantId, l.Email }).HasName("IX_Lead_Tenant_Email");
                eb.Property(l => l.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Program (domain) - use alias to avoid collision with app Program
            modelBuilder.Entity<DomainProgram>(eb =>
            {
                eb.HasKey(p => p.Id);
                eb.Property(p => p.Name).IsRequired().HasMaxLength(200);
                eb.Property(p => p.IsActive).HasDefaultValue(true);
            });

            // Enrollment - relationships to Patient and Program
            modelBuilder.Entity<Enrollment>(eb =>
            {
                eb.HasKey(e => e.Id);
                eb.HasOne(e => e.Patient)
                  .WithMany()
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasOne(e => e.Program)
                  .WithMany()
                  .HasForeignKey(e => e.ProgramId)
                  .OnDelete(DeleteBehavior.NoAction);
            });

            // Tenant and TenantConfiguration
            modelBuilder.Entity<Tenant>(eb =>
            {
                eb.HasKey(t => t.Id);
                eb.Property(t => t.Name).IsRequired().HasMaxLength(200);
                eb.HasIndex(t => t.Hostname).HasName("IX_Tenant_Hostname");
            });

            modelBuilder.Entity<TenantConfiguration>(eb =>
            {
                eb.HasKey(tc => tc.Id);
                eb.Property(tc => tc.Key).IsRequired().HasMaxLength(200);
                eb.HasOne(tc => tc.Tenant)
                  .WithMany()
                  .HasForeignKey(tc => tc.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
                eb.HasIndex(tc => new { tc.TenantId, tc.Key }).IsUnique().HasName("UX_TenantConfig_Tenant_Key");
            });

            // FollowUpRule
            modelBuilder.Entity<FollowUpRule>(eb =>
            {
                eb.HasKey(f => f.Id);
                eb.Property(f => f.Name).IsRequired().HasMaxLength(200);
                eb.Property(f => f.IntervalDays).HasDefaultValue(0);
            });

            // TaskItem - Enhanced with Phase 2 properties
            modelBuilder.Entity<TaskItem>(eb =>
            {
                eb.HasKey(t => t.Id);
                eb.Property(t => t.TenantId).IsRequired();
                eb.Property(t => t.Title).IsRequired().HasMaxLength(200);
                eb.Property(t => t.TaskType).HasMaxLength(50);
                eb.Property(t => t.Status).HasMaxLength(50);
                eb.Property(t => t.Priority).HasMaxLength(50);
                eb.Property(t => t.CreationSource).HasMaxLength(50);
                eb.Property(t => t.Outcome).HasMaxLength(50);
                eb.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                eb.HasOne(t => t.Patient)
                  .WithMany()
                  .HasForeignKey(t => t.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasOne(t => t.Enrollment)
                  .WithMany()
                  .HasForeignKey(t => t.EnrollmentId)
                  .OnDelete(DeleteBehavior.SetNull);

                eb.HasOne(t => t.Program)
                  .WithMany()
                  .HasForeignKey(t => t.ProgramId)
                  .OnDelete(DeleteBehavior.SetNull);

                eb.HasIndex(t => new { t.TenantId, t.PatientId }).HasName("IX_TaskItem_Tenant_Patient");
                eb.HasIndex(t => new { t.TenantId, t.Status }).HasName("IX_TaskItem_Status");
                eb.HasIndex(t => t.DueDate).HasName("IX_TaskItem_DueDate");
                eb.HasIndex(t => new { t.TenantId, t.AssignedTo }).HasName("IX_TaskItem_Assigned");
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(eb =>
            {
                eb.HasKey(a => a.Id);
                eb.Property(a => a.EntityName).IsRequired().HasMaxLength(200);
                eb.Property(a => a.Action).IsRequired().HasMaxLength(100);
                eb.Property(a => a.ChangedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // ==================== CONFIGURABLE WORKFLOW ENTITIES (Phase 3.5) ====================

            // ProgramConfiguration
            modelBuilder.Entity<ProgramConfiguration>(eb =>
            {
                eb.HasKey(pc => pc.Id);
                eb.Property(pc => pc.TenantId).IsRequired();
                eb.Property(pc => pc.ProgramId).IsRequired();
                eb.Property(pc => pc.WorkflowType).IsRequired().HasMaxLength(50);

                eb.HasOne(pc => pc.Program)
                  .WithMany()
                  .HasForeignKey(pc => pc.ProgramId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasIndex(pc => new { pc.TenantId, pc.ProgramId }).HasName("IX_ProgramConfig_Tenant_Program");
            });

            // ProcessStage
            modelBuilder.Entity<ProcessStage>(eb =>
            {
                eb.HasKey(ps => ps.Id);
                eb.Property(ps => ps.TenantId).IsRequired();
                eb.Property(ps => ps.Name).IsRequired().HasMaxLength(200);
                eb.Property(ps => ps.DefaultStatus).HasMaxLength(50);

                eb.HasOne(ps => ps.ProgramConfiguration)
                  .WithMany(pc => pc.ProcessStages)
                  .HasForeignKey(ps => ps.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasIndex(ps => new { ps.TenantId, ps.ProgramConfigurationId, ps.Order })
                  .HasName("IX_ProcessStage_Config_Order");
            });

            // PatientPathway
            modelBuilder.Entity<PatientPathway>(eb =>
            {
                eb.HasKey(pp => pp.Id);
                eb.Property(pp => pp.TenantId).IsRequired();
                eb.Property(pp => pp.CurrentStatus).HasMaxLength(50);

                eb.HasOne(pp => pp.Patient)
                  .WithMany()
                  .HasForeignKey(pp => pp.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasOne(pp => pp.Enrollment)
                  .WithMany()
                  .HasForeignKey(pp => pp.EnrollmentId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasOne(pp => pp.ProgramConfiguration)
                  .WithMany(pc => pc.PatientPathways)
                  .HasForeignKey(pp => pp.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.Restrict);

                eb.HasOne(pp => pp.CurrentProcessStage)
                  .WithMany(ps => ps.PatientPathways)
                  .HasForeignKey(pp => pp.CurrentProcessStageId)
                  .OnDelete(DeleteBehavior.Restrict);

                eb.HasIndex(pp => new { pp.TenantId, pp.PatientId }).HasName("IX_PatientPathway_Tenant_Patient");
                eb.HasIndex(pp => new { pp.TenantId, pp.EnrollmentId }).HasName("IX_PatientPathway_Enrollment");
                eb.HasIndex(pp => new { pp.TenantId, pp.CurrentProcessStageId }).HasName("IX_PatientPathway_Stage");
            });

            // FormDefinition
            modelBuilder.Entity<FormDefinition>(eb =>
            {
                eb.HasKey(fd => fd.Id);
                eb.Property(fd => fd.TenantId).IsRequired();
                eb.Property(fd => fd.Title).IsRequired().HasMaxLength(200);
                eb.Property(fd => fd.FieldsDefinition).IsRequired();

                eb.HasOne(fd => fd.ProgramConfiguration)
                  .WithMany(pc => pc.FormDefinitions)
                  .HasForeignKey(fd => fd.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasIndex(fd => new { fd.TenantId, fd.ProgramConfigurationId }).HasName("IX_FormDef_Program");
            });

            // TaskDefinition
            modelBuilder.Entity<TaskDefinition>(eb =>
            {
                eb.HasKey(td => td.Id);
                eb.Property(td => td.TenantId).IsRequired();
                eb.Property(td => td.Title).IsRequired().HasMaxLength(200);
                eb.Property(td => td.TaskType).HasMaxLength(50);

                eb.HasOne(td => td.ProgramConfiguration)
                  .WithMany(pc => pc.TaskDefinitions)
                  .HasForeignKey(td => td.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasOne(td => td.ContentTemplate)
                  .WithMany()
                  .HasForeignKey(td => td.ContentTemplateId)
                  .OnDelete(DeleteBehavior.SetNull);
            });

            // ContentTemplate
            modelBuilder.Entity<ContentTemplate>(eb =>
            {
                eb.HasKey(ct => ct.Id);
                eb.Property(ct => ct.TenantId).IsRequired();
                eb.Property(ct => ct.Name).IsRequired().HasMaxLength(200);
                eb.Property(ct => ct.Content).IsRequired();
                eb.Property(ct => ct.ContentType).HasMaxLength(50);
                eb.Property(ct => ct.Language).HasMaxLength(10);

                eb.HasOne(ct => ct.ProgramConfiguration)
                  .WithMany(pc => pc.ContentTemplates)
                  .HasForeignKey(ct => ct.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasIndex(ct => new { ct.TenantId, ct.ProgramConfigurationId, ct.ContentType })
                  .HasName("IX_ContentTemplate_Type");
            });

            // BusinessRule
            modelBuilder.Entity<BusinessRule>(eb =>
            {
                eb.HasKey(br => br.Id);
                eb.Property(br => br.TenantId).IsRequired();
                eb.Property(br => br.Name).IsRequired().HasMaxLength(200);
                eb.Property(br => br.RuleType).HasMaxLength(50);
                eb.Property(br => br.TriggerEvent).HasMaxLength(100);
                eb.Property(br => br.Condition).IsRequired();
                eb.Property(br => br.Action).IsRequired();

                eb.HasOne(br => br.ProgramConfiguration)
                  .WithMany(pc => pc.BusinessRules)
                  .HasForeignKey(br => br.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasIndex(br => new { br.TenantId, br.TriggerEvent, br.IsActive })
                  .HasName("IX_BusinessRule_Trigger");
            });

            // RolePermission
            modelBuilder.Entity<RolePermission>(eb =>
            {
                eb.HasKey(rp => rp.Id);
                eb.Property(rp => rp.TenantId).IsRequired();
                eb.Property(rp => rp.RoleName).IsRequired().HasMaxLength(100);

                eb.HasOne(rp => rp.ProgramConfiguration)
                  .WithMany()
                  .HasForeignKey(rp => rp.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.NoAction);

                eb.HasIndex(rp => new { rp.TenantId, rp.RoleName }).HasName("IX_RolePermission_TenantRole");
            });

            // ==================== PHASE 2 EXTENDED ENTITIES ====================

            // WorkflowEvent - Timeline tracking
            modelBuilder.Entity<WorkflowEvent>(eb =>
            {
                eb.HasKey(we => we.Id);
                eb.Property(we => we.TenantId).IsRequired();
                eb.Property(we => we.EventType).IsRequired().HasMaxLength(100);
                eb.Property(we => we.Description).IsRequired();
                eb.Property(we => we.TriggeredBy).HasMaxLength(200);
                eb.Property(we => we.EventOccurredAt).IsRequired();

                eb.HasOne(we => we.PatientPathway)
                  .WithMany()
                  .HasForeignKey(we => we.PatientPathwayId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasIndex(we => new { we.TenantId, we.PatientPathwayId }).HasName("IX_WorkflowEvent_Pathway");
                eb.HasIndex(we => new { we.TenantId, we.EventOccurredAt }).HasName("IX_WorkflowEvent_Timeline");
                eb.HasIndex(we => new { we.TenantId, we.EventType, we.VisibleToPatient }).HasName("IX_WorkflowEvent_Type_Visibility");
            });

            // Questionnaire - Survey scheduling
            modelBuilder.Entity<Questionnaire>(eb =>
            {
                eb.HasKey(q => q.Id);
                eb.Property(q => q.TenantId).IsRequired();
                eb.Property(q => q.Title).IsRequired().HasMaxLength(200);
                eb.Property(q => q.RecurrenceType).HasMaxLength(50);
                eb.Property(q => q.TriggerEventType).HasMaxLength(100);

                eb.HasOne(q => q.ProgramConfiguration)
                  .WithMany()
                  .HasForeignKey(q => q.ProgramConfigurationId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasIndex(q => new { q.TenantId, q.ProgramConfigurationId }).HasName("IX_Questionnaire_Program");
                eb.HasIndex(q => new { q.TenantId, q.IsActive, q.RecurrenceType }).HasName("IX_Questionnaire_Active_Recurrence");
            });

            // QuestionnaireResponse - Answer tracking
            modelBuilder.Entity<QuestionnaireResponse>(eb =>
            {
                eb.HasKey(qr => qr.Id);
                eb.Property(qr => qr.TenantId).IsRequired();
                eb.Property(qr => qr.ResponseData).IsRequired();
                eb.Property(qr => qr.Status).HasMaxLength(50);

                eb.HasOne(qr => qr.Questionnaire)
                  .WithMany(q => q.Responses)
                  .HasForeignKey(qr => qr.QuestionnaireId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasOne(qr => qr.PatientPathway)
                  .WithMany()
                  .HasForeignKey(qr => qr.PatientPathwayId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasIndex(qr => new { qr.TenantId, qr.PatientPathwayId }).HasName("IX_QuestionnaireResponse_Pathway");
                eb.HasIndex(qr => new { qr.TenantId, qr.QuestionnaireId, qr.Status }).HasName("IX_QuestionnaireResponse_Status");
            });

            // TriggerExecution - Retry/dedup tracking
            modelBuilder.Entity<TriggerExecution>(eb =>
            {
                eb.HasKey(te => te.Id);
                eb.Property(te => te.TenantId).IsRequired();
                eb.Property(te => te.TriggerType).IsRequired().HasMaxLength(100);
                eb.Property(te => te.Status).HasMaxLength(50);
                eb.Property(te => te.DeduplicationKey).HasMaxLength(250);
                eb.Property(te => te.ExecutionResult).HasMaxLength(200);

                eb.HasOne(te => te.PatientPathway)
                  .WithMany()
                  .HasForeignKey(te => te.PatientPathwayId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasIndex(te => new { te.TenantId, te.PatientPathwayId }).HasName("IX_TriggerExecution_Pathway");
                eb.HasIndex(te => new { te.TenantId, te.Status, te.NextRetryAt }).HasName("IX_TriggerExecution_Pending");
                eb.HasIndex(te => new { te.TenantId, te.DeduplicationKey }).HasName("IX_TriggerExecution_Dedup");
                eb.HasIndex(te => te.ScheduledAt).HasName("IX_TriggerExecution_Scheduled");
            });

            // CommunicationMessage - Multi-channel message queuing
            modelBuilder.Entity<CommunicationMessage>(eb =>
            {
                eb.HasKey(cm => cm.Id);
                eb.Property(cm => cm.TenantId).IsRequired();
                eb.Property(cm => cm.Channel).IsRequired().HasMaxLength(50);
                eb.Property(cm => cm.Recipient).IsRequired().HasMaxLength(200);
                eb.Property(cm => cm.Subject).HasMaxLength(200);
                eb.Property(cm => cm.MessageBody).IsRequired();
                eb.Property(cm => cm.MessageType).HasMaxLength(50);
                eb.Property(cm => cm.Status).HasMaxLength(50);
                eb.Property(cm => cm.ProviderMessageId).HasMaxLength(200);
                eb.Property(cm => cm.RelatedEntityType).HasMaxLength(50);

                eb.HasOne(cm => cm.PatientPathway)
                  .WithMany()
                  .HasForeignKey(cm => cm.PatientPathwayId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasIndex(cm => new { cm.TenantId, cm.Status, cm.ScheduledFor }).HasName("IX_CommunicationMessage_Pending");
                eb.HasIndex(cm => new { cm.TenantId, cm.PatientPathwayId }).HasName("IX_CommunicationMessage_Pathway");
                eb.HasIndex(cm => new { cm.TenantId, cm.Channel }).HasName("IX_CommunicationMessage_Channel");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
