using System;
using System.Linq;
using BOnlineHomeAssignement.Server.Domain.Entities;

namespace BOnlineHomeAssignement.Server.Infrastructure.Persistence
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            if (db.Tenants.Any()) return; // already seeded

            // Create sample tenant
            var tenantId = Guid.Parse("8a3d9c2a-1111-4f3b-8c2e-000000000001");
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = "Default Tenant",
                Hostname = "localhost"
            };
            db.Tenants.Add(tenant);

            // Tenant configuration
            db.TenantConfigurations.Add(new TenantConfiguration
            {
                Id = Guid.Parse("8a3d9c2a-1111-4f3b-8c2e-000000000002"),
                TenantId = tenantId,
                Key = "SupportEmail",
                Value = "support@example.com"
            });

            // Programs
            var programId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var program = new BOnlineHomeAssignement.Server.Domain.Entities.Program
            {
                Id = programId,
                Name = "Basic Care Program",
                Description = "A sample care program",
                StartDate = DateTime.UtcNow.Date,
                IsActive = true
            };
            db.Set<BOnlineHomeAssignement.Server.Domain.Entities.Program>().Add(program);

            // Patients
            var patientId = Guid.Parse("22222222-3333-4444-5555-666666666666");
            var patient = new Patient
            {
                Id = patientId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-0100",
                CreatedAt = DateTime.UtcNow
            };
            db.Patients.Add(patient);

            // Lead
            var leadId = Guid.Parse("33333333-4444-5555-6666-777777777777");
            db.Leads.Add(new Lead
            {
                Id = leadId,
                Name = "Jane Prospect",
                Email = "jane.prospect@example.com",
                CreatedAt = DateTime.UtcNow,
                Source = "Website"
            });

            // FollowUpRule
            db.FollowUpRules.Add(new FollowUpRule
            {
                Id = Guid.Parse("44444444-5555-6666-7777-888888888888"),
                Name = "Weekly Check-in",
                Description = "Follow up weekly",
                IntervalDays = 7,
                IsActive = true
            });

            // TaskItem
            db.TaskItems.Add(new TaskItem
            {
                Id = Guid.Parse("55555555-6666-7777-8888-999999999999"),
                Title = "Initial Assessment",
                Description = "Perform initial patient assessment",
                AssignedTo = "nurse@example.com",
                CreatedAt = DateTime.UtcNow
            });

            // Enrollment linking patient to program
            db.Enrollments.Add(new Enrollment
            {
                Id = Guid.Parse("66666666-7777-8888-9999-000000000000"),
                PatientId = patientId,
                ProgramId = programId,
                EnrolledAt = DateTime.UtcNow,
                Status = "Active"
            });

            // AuditLog
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.Parse("77777777-8888-9999-0000-111111111111"),
                EntityName = "Patient",
                EntityId = patientId,
                Action = "Created",
                ChangedBy = "system",
                ChangedAt = DateTime.UtcNow,
                Changes = "{\"FirstName\":\"John\"}"
            });

            // ==================== WORKFLOW CONFIGURATION (Phase 3.5) ====================
            // Create a sample program configuration with stages, forms, rules, and templates

            var programConfigId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var programConfig = new ProgramConfiguration
            {
                Id = programConfigId,
                TenantId = tenantId,
                ProgramId = programId,
                WorkflowType = "Linear",
                PatientTypes = "Initial,Advanced,Chronic",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.ProgramConfigurations.Add(programConfig);

            // Add process stages
            var stage1Id = Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111");
            var stage2Id = Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222");
            var stage3Id = Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333");

            db.ProcessStages.AddRange(
                new ProcessStage
                {
                    Id = stage1Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Initial Assessment",
                    Order = 1,
                    DurationDays = 7,
                    Description = "Patient completes initial assessment and intake forms",
                    DefaultStatus = "Active",
                    AllowedStatuses = "Active,Pending,Completed,Skipped",
                    RequiredFormIds = null, // Will be set after forms are created
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ProcessStage
                {
                    Id = stage2Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Treatment Planning",
                    Order = 2,
                    DurationDays = 14,
                    Description = "Care team develops personalized treatment plan",
                    DefaultStatus = "Active",
                    AllowedStatuses = "Active,PendingApproval,Approved,Completed",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ProcessStage
                {
                    Id = stage3Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Follow-Up",
                    Order = 3,
                    DurationDays = 30,
                    Description = "Regular follow-up appointments and progress monitoring",
                    DefaultStatus = "Active",
                    AllowedStatuses = "Active,OnHold,Completed,Discharged",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            // Add form definitions
            var form1Id = Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444");
            var form2Id = Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555");

            db.FormDefinitions.AddRange(
                new FormDefinition
                {
                    Id = form1Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Title = "Initial Health Assessment",
                    Description = "Comprehensive health history and current status",
                    FieldsDefinition = "[{\"name\":\"medicalHistory\",\"type\":\"textarea\",\"required\":true},{\"name\":\"allergies\",\"type\":\"text\",\"required\":false},{\"name\":\"currentMedications\",\"type\":\"textarea\",\"required\":true},{\"name\":\"concernAreas\",\"type\":\"checkbox\",\"options\":[\"Mental Health\",\"Physical Health\",\"Social Support\"],\"required\":true}]",
                    ApplicableStages = "Initial Assessment",
                    IsRequired = true,
                    IsActive = true,
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new FormDefinition
                {
                    Id = form2Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Title = "Treatment Plan Feedback",
                    Description = "Patient feedback on proposed treatment plan",
                    FieldsDefinition = "[{\"name\":\"acceptanceName\",\"type\":\"radio\",\"options\":[\"Agree\",\"Partially Agree\",\"Disagree\"],\"required\":true},{\"name\":\"concerns\",\"type\":\"textarea\",\"required\":false},{\"name\":\"adjustmentRequests\",\"type\":\"textarea\",\"required\":false}]",
                    ApplicableStages = "Treatment Planning",
                    IsRequired = true,
                    IsActive = true,
                    Order = 1,
                    CreatedAt = DateTime.UtcNow
                });

            // Add content templates for messaging
            var emailTemplate1 = Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666");
            var emailTemplate2 = Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777");

            db.ContentTemplates.AddRange(
                new ContentTemplate
                {
                    Id = emailTemplate1,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Initial Assessment Due",
                    ContentType = "Email",
                    Subject = "Your Initial Health Assessment is due",
                    Content = "Dear {{patientName}}, Please complete your initial health assessment by {{dueDate}}. This will help us understand your needs and create a personalized care plan. Log in to your patient portal to complete the assessment.",
                    SupportedVariables = "patientName,dueDate,stageName",
                    Language = "en",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ContentTemplate
                {
                    Id = emailTemplate2,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Treatment Plan Ready",
                    ContentType = "Email",
                    Subject = "Your personalized treatment plan is ready",
                    Content = "Dear {{patientName}}, Your care team has completed your treatment plan. Please review it in your patient portal and provide your feedback by {{dueDate}}.",
                    SupportedVariables = "patientName,dueDate",
                    Language = "en",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            // Add business rules for workflow automation
            var rule1Id = Guid.Parse("88888888-aaaa-bbbb-cccc-888888888888");
            var rule2Id = Guid.Parse("99999999-aaaa-bbbb-cccc-999999999999");

            db.BusinessRules.AddRange(
                new BusinessRule
                {
                    Id = rule1Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Auto-advance on assessment complete",
                    RuleType = "StageTransition",
                    TriggerEvent = "OnFormSubmit",
                    Condition = "{\"formId\":\"" + form1Id + "\"}",
                    Action = "{\"type\":\"transition\",\"targetStage\":\"" + stage2Id + "\"}",
                    Priority = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new BusinessRule
                {
                    Id = rule2Id,
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    Name = "Send reminder if assessment overdue",
                    RuleType = "Notification",
                    TriggerEvent = "Daily",
                    Condition = "{\"daysOverdue\":3}",
                    Action = "{\"type\":\"sendNotification\",\"templateId\":\"" + emailTemplate1 + "\"}",
                    Priority = 5,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            // Add role definitions
            db.RolePermissions.AddRange(
                new RolePermission
                {
                    Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-aaaaaaaaaaaa"),
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    RoleName = "Patient",
                    AccessibleStages = stage1Id + "," + stage2Id + "," + stage3Id,
                    Permissions = "Read,SubmitForms",
                    AssignableTaskTypes = null,
                    CanViewPathways = true,
                    CanApproveTransitions = false,
                    CanOverrideRules = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new RolePermission
                {
                    Id = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-bbbbbbbbbbbb"),
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    RoleName = "Nurse",
                    AccessibleStages = stage1Id + "," + stage2Id + "," + stage3Id,
                    Permissions = "Read,Create,Edit,SubmitForms",
                    AssignableTaskTypes = "Assessment,FollowUp",
                    CanViewPathways = true,
                    CanApproveTransitions = true,
                    CanOverrideRules = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new RolePermission
                {
                    Id = Guid.Parse("cccccccc-dddd-eeee-ffff-cccccccccccc"),
                    TenantId = tenantId,
                    ProgramConfigurationId = programConfigId,
                    RoleName = "Admin",
                    AccessibleStages = stage1Id + "," + stage2Id + "," + stage3Id,
                    Permissions = "Read,Create,Edit,Delete,Approve,Escalate",
                    AssignableTaskTypes = "Assessment,Treatment,FollowUp,Escalation",
                    CanViewPathways = true,
                    CanApproveTransitions = true,
                    CanOverrideRules = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

            db.SaveChanges();
        }
    }
}
