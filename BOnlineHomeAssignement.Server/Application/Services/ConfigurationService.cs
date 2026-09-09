using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using DomainProgram = BOnlineHomeAssignement.Server.Domain.Entities.Program;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Configuration management service for defining programs, stages, forms, rules, and content.
    /// Allows customers to build custom workflow configurations without code changes.
    /// </summary>
    public interface IConfigurationService
    {
        // Program Configuration
        Task<ProgramConfiguration> CreateProgramConfigurationAsync(
            Guid tenantId, Guid programId, string workflowType = "Linear");

        Task<ProgramConfiguration?> GetProgramConfigurationAsync(
            Guid tenantId, Guid programConfigurationId);

        Task UpdateProgramConfigurationAsync(
            Guid tenantId, Guid programConfigurationId, ProgramConfiguration config);

        // Process Stages
        Task<ProcessStage> AddProcessStageAsync(
            Guid tenantId, Guid programConfigurationId, string stageName, int order,
            int? durationDays = null, string? description = null);

        Task<List<ProcessStage>> GetProgramStagesAsync(
            Guid tenantId, Guid programConfigurationId);

        Task UpdateProcessStageAsync(
            Guid tenantId, Guid stageId, ProcessStage stage);

        // Form Definitions
        Task<FormDefinition> CreateFormDefinitionAsync(
            Guid tenantId, Guid programConfigurationId, string title,
            string fieldsJson, string? applicableStages = null);

        Task<List<FormDefinition>> GetProgramFormsAsync(
            Guid tenantId, Guid programConfigurationId);

        Task UpdateFormDefinitionAsync(
            Guid tenantId, Guid formId, FormDefinition form);

        // Task Definitions
        Task<TaskDefinition> CreateTaskDefinitionAsync(
            Guid tenantId, Guid programConfigurationId, string title,
            string taskType = "Manual", int dueDays = 7);

        Task<List<TaskDefinition>> GetProgramTaskDefinitionsAsync(
            Guid tenantId, Guid programConfigurationId);

        // Business Rules
        Task<BusinessRule> CreateBusinessRuleAsync(
            Guid tenantId, Guid programConfigurationId, string ruleName,
            string ruleType, string triggerEvent, string conditionJson, string actionJson);

        Task<List<BusinessRule>> GetProgramRulesAsync(
            Guid tenantId, Guid programConfigurationId);

        // Content Templates
        Task<ContentTemplate> CreateContentTemplateAsync(
            Guid tenantId, Guid programConfigurationId, string name,
            string contentType, string content, string? subject = null);

        Task<List<ContentTemplate>> GetProgramTemplatesAsync(
            Guid tenantId, Guid programConfigurationId, string? contentType = null);

        // Role Permissions
        Task<RolePermission> CreateRolePermissionAsync(
            Guid tenantId, Guid programConfigurationId, string roleName,
            string? accessibleStages = null, string? permissions = null);

        Task<List<RolePermission>> GetProgramRolesAsync(
            Guid tenantId, Guid programConfigurationId);

        Task<bool> RoleCanAccessStageAsync(
            Guid tenantId, Guid programConfigurationId, string roleName, Guid stageId);

        // Bulk Operations
        Task<ProgramConfiguration> ImportConfigurationAsync(
            Guid tenantId, Guid programId, string configurationJson);

        Task<string> ExportConfigurationAsync(
            Guid tenantId, Guid programConfigurationId);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IRepository<ProgramConfiguration> _programConfigRepository;
        private readonly IRepository<ProcessStage> _processStageRepository;
        private readonly IRepository<FormDefinition> _formRepository;
        private readonly IRepository<TaskDefinition> _taskDefRepository;
        private readonly IRepository<BusinessRule> _ruleRepository;
        private readonly IRepository<ContentTemplate> _templateRepository;
        private readonly IRepository<RolePermission> _roleRepository;
        private readonly IRepository<DomainProgram> _programRepository;

        public ConfigurationService(
            IRepository<ProgramConfiguration> programConfigRepository,
            IRepository<ProcessStage> processStageRepository,
            IRepository<FormDefinition> formRepository,
            IRepository<TaskDefinition> taskDefRepository,
            IRepository<BusinessRule> ruleRepository,
            IRepository<ContentTemplate> templateRepository,
            IRepository<RolePermission> roleRepository,
            IRepository<DomainProgram> programRepository)
        {
            _programConfigRepository = programConfigRepository;
            _processStageRepository = processStageRepository;
            _formRepository = formRepository;
            _taskDefRepository = taskDefRepository;
            _ruleRepository = ruleRepository;
            _templateRepository = templateRepository;
            _roleRepository = roleRepository;
            _programRepository = programRepository;
        }

        // ==================== PROGRAM CONFIGURATION ====================

        public async Task<ProgramConfiguration> CreateProgramConfigurationAsync(
            Guid tenantId, Guid programId, string workflowType = "Linear")
        {
            var program = await _programRepository.GetByIdAsync(programId);
            if (program == null)
                throw new InvalidOperationException("Program not found");

            var config = new ProgramConfiguration
            {
                TenantId = tenantId,
                ProgramId = programId,
                WorkflowType = workflowType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _programConfigRepository.AddAsync(config);
            await _programConfigRepository.SaveChangesAsync();

            return config;
        }

        public async Task<ProgramConfiguration?> GetProgramConfigurationAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            return config?.TenantId == tenantId ? config : null;
        }

        public async Task UpdateProgramConfigurationAsync(
            Guid tenantId, Guid programConfigurationId, ProgramConfiguration config)
        {
            var existing = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (existing == null || existing.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found or unauthorized");

            existing.WorkflowType = config.WorkflowType;
            existing.WorkflowDefinition = config.WorkflowDefinition;
            existing.DefaultBusinessRules = config.DefaultBusinessRules;
            existing.PatientTypes = config.PatientTypes;
            existing.IsActive = config.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _programConfigRepository.UpdateAsync(existing);
            await _programConfigRepository.SaveChangesAsync();
        }

        // ==================== PROCESS STAGES ====================

        public async Task<ProcessStage> AddProcessStageAsync(
            Guid tenantId, Guid programConfigurationId, string stageName, int order,
            int? durationDays = null, string? description = null)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (config == null || config.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found");

            var stage = new ProcessStage
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                Name = stageName,
                Order = order,
                DurationDays = durationDays,
                Description = description,
                DefaultStatus = "Active",
                AllowedStatuses = "Active,Pending,Completed,Skipped",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _processStageRepository.AddAsync(stage);
            await _processStageRepository.SaveChangesAsync();

            return stage;
        }

        public async Task<List<ProcessStage>> GetProgramStagesAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var results = await _processStageRepository.FindAsync(
                ps => ps.TenantId == tenantId
                   && ps.ProgramConfigurationId == programConfigurationId
                   && ps.IsActive);
            return results.OrderBy(ps => ps.Order).ToList();
        }

        public async Task UpdateProcessStageAsync(
            Guid tenantId, Guid stageId, ProcessStage stage)
        {
            var existing = await _processStageRepository.GetByIdAsync(stageId);
            if (existing == null || existing.TenantId != tenantId)
                throw new InvalidOperationException("Stage not found or unauthorized");

            existing.Name = stage.Name;
            existing.Description = stage.Description;
            existing.DurationDays = stage.DurationDays;
            existing.AllowedStatuses = stage.AllowedStatuses;
            existing.DefaultStatus = stage.DefaultStatus;
            existing.RequiredFormIds = stage.RequiredFormIds;
            existing.TriggeredTaskIds = stage.TriggeredTaskIds;
            existing.AutoAdvanceConditions = stage.AutoAdvanceConditions;
            existing.IsActive = stage.IsActive;

            await _processStageRepository.UpdateAsync(existing);
            await _processStageRepository.SaveChangesAsync();
        }

        // ==================== FORM DEFINITIONS ====================

        public async Task<FormDefinition> CreateFormDefinitionAsync(
            Guid tenantId, Guid programConfigurationId, string title,
            string fieldsJson, string? applicableStages = null)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (config == null || config.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found");

            var form = new FormDefinition
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                Title = title,
                FieldsDefinition = fieldsJson,
                ApplicableStages = applicableStages,
                IsRequired = true,
                IsActive = true,
                Order = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _formRepository.AddAsync(form);
            await _formRepository.SaveChangesAsync();

            return form;
        }

        public async Task<List<FormDefinition>> GetProgramFormsAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var results = await _formRepository.FindAsync(
                f => f.TenantId == tenantId
                   && f.ProgramConfigurationId == programConfigurationId
                   && f.IsActive);
            return results.OrderBy(f => f.Order).ToList();
        }

        public async Task UpdateFormDefinitionAsync(
            Guid tenantId, Guid formId, FormDefinition form)
        {
            var existing = await _formRepository.GetByIdAsync(formId);
            if (existing == null || existing.TenantId != tenantId)
                throw new InvalidOperationException("Form not found or unauthorized");

            existing.Title = form.Title;
            existing.Description = form.Description;
            existing.FieldsDefinition = form.FieldsDefinition;
            existing.ApplicableStages = form.ApplicableStages;
            existing.VisibilityConditions = form.VisibilityConditions;
            existing.OnSubmitActions = form.OnSubmitActions;
            existing.IsRequired = form.IsRequired;
            existing.IsActive = form.IsActive;

            await _formRepository.UpdateAsync(existing);
            await _formRepository.SaveChangesAsync();
        }

        // ==================== TASK DEFINITIONS ====================

        public async Task<TaskDefinition> CreateTaskDefinitionAsync(
            Guid tenantId, Guid programConfigurationId, string title,
            string taskType = "Manual", int dueDays = 7)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (config == null || config.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found");

            var taskDef = new TaskDefinition
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                Title = title,
                TaskType = taskType,
                DueDays = dueDays,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _taskDefRepository.AddAsync(taskDef);
            await _taskDefRepository.SaveChangesAsync();

            return taskDef;
        }

        public async Task<List<TaskDefinition>> GetProgramTaskDefinitionsAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var results = await _taskDefRepository.FindAsync(
                td => td.TenantId == tenantId
                   && td.ProgramConfigurationId == programConfigurationId
                   && td.IsActive);
            return results.ToList();
        }

        // ==================== BUSINESS RULES ====================

        public async Task<BusinessRule> CreateBusinessRuleAsync(
            Guid tenantId, Guid programConfigurationId, string ruleName,
            string ruleType, string triggerEvent, string conditionJson, string actionJson)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (config == null || config.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found");

            var rule = new BusinessRule
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                Name = ruleName,
                RuleType = ruleType,
                TriggerEvent = triggerEvent,
                Condition = conditionJson,
                Action = actionJson,
                Priority = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _ruleRepository.AddAsync(rule);
            await _ruleRepository.SaveChangesAsync();

            return rule;
        }

        public async Task<List<BusinessRule>> GetProgramRulesAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var results = await _ruleRepository.FindAsync(
                br => br.TenantId == tenantId
                   && br.ProgramConfigurationId == programConfigurationId
                   && br.IsActive);
            return results.OrderBy(br => br.Priority).ToList();
        }

        // ==================== CONTENT TEMPLATES ====================

        public async Task<ContentTemplate> CreateContentTemplateAsync(
            Guid tenantId, Guid programConfigurationId, string name,
            string contentType, string content, string? subject = null)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (config == null || config.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found");

            var template = new ContentTemplate
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                Name = name,
                ContentType = contentType,
                Content = content,
                Subject = subject,
                Language = "en",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _templateRepository.AddAsync(template);
            await _templateRepository.SaveChangesAsync();

            return template;
        }

        public async Task<List<ContentTemplate>> GetProgramTemplatesAsync(
            Guid tenantId, Guid programConfigurationId, string? contentType = null)
        {
            if (contentType == null)
            {
                var results = await _templateRepository.FindAsync(
                    ct => ct.TenantId == tenantId
                       && ct.ProgramConfigurationId == programConfigurationId
                       && ct.IsActive);
                return results.ToList();
            }
            else
            {
                var results = await _templateRepository.FindAsync(
                    ct => ct.TenantId == tenantId
                       && ct.ProgramConfigurationId == programConfigurationId
                       && ct.ContentType == contentType
                       && ct.IsActive);
                return results.ToList();
            }
        }

        // ==================== ROLE PERMISSIONS ====================

        public async Task<RolePermission> CreateRolePermissionAsync(
            Guid tenantId, Guid programConfigurationId, string roleName,
            string? accessibleStages = null, string? permissions = null)
        {
            var config = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (config == null || config.TenantId != tenantId)
                throw new InvalidOperationException("Configuration not found");

            var role = new RolePermission
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                RoleName = roleName,
                AccessibleStages = accessibleStages,
                Permissions = permissions,
                CanViewPathways = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _roleRepository.AddAsync(role);
            await _roleRepository.SaveChangesAsync();

            return role;
        }

        public async Task<List<RolePermission>> GetProgramRolesAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var results = await _roleRepository.FindAsync(
                rp => rp.TenantId == tenantId
                   && rp.ProgramConfigurationId == programConfigurationId
                   && rp.IsActive);
            return results.ToList();
        }

        public async Task<bool> RoleCanAccessStageAsync(
            Guid tenantId, Guid programConfigurationId, string roleName, Guid stageId)
        {
            var roles = await _roleRepository.FindAsync(
                rp => rp.TenantId == tenantId
                   && rp.ProgramConfigurationId == programConfigurationId
                   && rp.RoleName == roleName
                   && rp.IsActive);

            var role = roles.FirstOrDefault();
            if (role == null)
                return false;

            if (string.IsNullOrEmpty(role.AccessibleStages))
                return true; // No restrictions

            return role.AccessibleStages.Contains(stageId.ToString());
        }

        // ==================== BULK OPERATIONS ====================

        public async Task<ProgramConfiguration> ImportConfigurationAsync(
            Guid tenantId, Guid programId, string configurationJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ProgramConfiguration>(configurationJson)
                    ?? throw new InvalidOperationException("Invalid configuration JSON");

                config.TenantId = tenantId;
                config.ProgramId = programId;
                config.Id = Guid.NewGuid();
                config.CreatedAt = DateTime.UtcNow;

                await _programConfigRepository.AddAsync(config);
                await _programConfigRepository.SaveChangesAsync();

                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse configuration JSON", ex);
            }
        }

        public async Task<string> ExportConfigurationAsync(
            Guid tenantId, Guid programConfigurationId)
        {
            var config = await GetProgramConfigurationAsync(tenantId, programConfigurationId);
            if (config == null)
                throw new InvalidOperationException("Configuration not found");

            var stages = await GetProgramStagesAsync(tenantId, programConfigurationId);
            var forms = await GetProgramFormsAsync(tenantId, programConfigurationId);
            var tasks = await GetProgramTaskDefinitionsAsync(tenantId, programConfigurationId);
            var rules = await GetProgramRulesAsync(tenantId, programConfigurationId);
            var templates = await GetProgramTemplatesAsync(tenantId, programConfigurationId);
            var roles = await GetProgramRolesAsync(tenantId, programConfigurationId);

            var export = new
            {
                configuration = config,
                stages,
                forms,
                taskDefinitions = tasks,
                businessRules = rules,
                contentTemplates = templates,
                rolePermissions = roles,
                exportedAt = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
