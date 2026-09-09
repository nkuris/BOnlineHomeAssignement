using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BOnlineHomeAssignement.Server.Application.Services;
using BOnlineHomeAssignement.Server.Application.DTOs;
using BOnlineHomeAssignement.Server.Domain.Entities;

namespace BOnlineHomeAssignement.Server.Controllers
{
    /// <summary>
    /// API endpoints for managing customer program configurations.
    /// Allows customers to define their own workflows, stages, forms, rules, and content.
    /// All operations are tenant-scoped based on X-Tenant-Id header.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configService;

        public ProgramConfigurationController(IConfigurationService configService)
        {
            _configService = configService;
        }

        private Guid GetTenantIdFromHeader()
        {
            var header = Request.Headers["X-Tenant-Id"].ToString();
            return Guid.TryParse(header, out var tenantId) ? tenantId : throw new UnauthorizedAccessException("Invalid or missing X-Tenant-Id header");
        }

        // ==================== PROGRAM CONFIGURATION ====================

        /// <summary>
        /// Create a new program configuration for a tenant's program.
        /// This is the entry point for defining a custom workflow/pathway system.
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<ProgramConfigurationDto>> CreateProgramConfiguration(
            [FromBody] CreateProgramConfigurationRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var config = await _configService.CreateProgramConfigurationAsync(
                    tenantId, request.ProgramId, request.WorkflowType ?? "Linear");

                return Ok(new ProgramConfigurationDto
                {
                    Id = config.Id,
                    ProgramId = config.ProgramId,
                    WorkflowType = config.WorkflowType,
                    IsActive = config.IsActive,
                    CreatedAt = config.CreatedAt
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get program configuration details.
        /// </summary>
        [HttpGet("{programConfigurationId}")]
        public async Task<ActionResult<ProgramConfigurationDto>> GetProgramConfiguration(Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var config = await _configService.GetProgramConfigurationAsync(tenantId, programConfigurationId);

                if (config == null)
                    return NotFound("Configuration not found");

                return Ok(new ProgramConfigurationDto
                {
                    Id = config.Id,
                    ProgramId = config.ProgramId,
                    WorkflowType = config.WorkflowType,
                    IsActive = config.IsActive,
                    CreatedAt = config.CreatedAt
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // ==================== PROCESS STAGES ====================

        /// <summary>
        /// Add a stage to a program workflow.
        /// Example: "Initial Assessment", "Treatment Planning", "Follow-up", "Discharge"
        /// </summary>
        [HttpPost("{programConfigurationId}/stages")]
        public async Task<ActionResult<ProcessStageDto>> AddProcessStage(
            Guid programConfigurationId,
            [FromBody] CreateProcessStageRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var stage = await _configService.AddProcessStageAsync(
                    tenantId, programConfigurationId, request.Name, request.Order,
                    request.DurationDays, request.Description);

                return Ok(new ProcessStageDto
                {
                    Id = stage.Id,
                    Name = stage.Name,
                    Order = stage.Order,
                    DurationDays = stage.DurationDays,
                    Description = stage.Description,
                    DefaultStatus = stage.DefaultStatus,
                    AllowedStatuses = stage.AllowedStatuses,
                    CreatedAt = stage.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all stages for a program.
        /// Returns stages in order.
        /// </summary>
        [HttpGet("{programConfigurationId}/stages")]
        public async Task<ActionResult<List<ProcessStageDto>>> GetProgramStages(Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var stages = await _configService.GetProgramStagesAsync(tenantId, programConfigurationId);

                var stageDtos = new List<ProcessStageDto>();
                foreach (var stage in stages)
                {
                    stageDtos.Add(new ProcessStageDto
                    {
                        Id = stage.Id,
                        Name = stage.Name,
                        Order = stage.Order,
                        DurationDays = stage.DurationDays,
                        Description = stage.Description,
                        DefaultStatus = stage.DefaultStatus,
                        AllowedStatuses = stage.AllowedStatuses,
                        CreatedAt = stage.CreatedAt
                    });
                }

                return Ok(stageDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== FORM DEFINITIONS ====================

        /// <summary>
        /// Create a form/questionnaire for a program stage.
        /// Forms collect patient data and can trigger workflow actions.
        /// </summary>
        [HttpPost("{programConfigurationId}/forms")]
        public async Task<ActionResult<FormDefinitionDto>> CreateFormDefinition(
            Guid programConfigurationId,
            [FromBody] CreateFormDefinitionRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var form = await _configService.CreateFormDefinitionAsync(
                    tenantId, programConfigurationId, request.Title,
                    request.FieldsDefinition, request.ApplicableStages);

                return Ok(new FormDefinitionDto
                {
                    Id = form.Id,
                    Title = form.Title,
                    Description = form.Description,
                    FieldsDefinition = form.FieldsDefinition,
                    ApplicableStages = form.ApplicableStages,
                    IsRequired = form.IsRequired,
                    CreatedAt = form.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all forms for a program.
        /// </summary>
        [HttpGet("{programConfigurationId}/forms")]
        public async Task<ActionResult<List<FormDefinitionDto>>> GetProgramForms(Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var forms = await _configService.GetProgramFormsAsync(tenantId, programConfigurationId);

                var formDtos = new List<FormDefinitionDto>();
                foreach (var form in forms)
                {
                    formDtos.Add(new FormDefinitionDto
                    {
                        Id = form.Id,
                        Title = form.Title,
                        Description = form.Description,
                        FieldsDefinition = form.FieldsDefinition,
                        ApplicableStages = form.ApplicableStages,
                        IsRequired = form.IsRequired,
                        CreatedAt = form.CreatedAt
                    });
                }

                return Ok(formDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== BUSINESS RULES ====================

        /// <summary>
        /// Create a business rule for workflow automation.
        /// Rules can trigger stage transitions, create tasks, send notifications, etc.
        /// Example: If assessmentScore > 70, auto-advance to next stage.
        /// </summary>
        [HttpPost("{programConfigurationId}/rules")]
        public async Task<ActionResult<BusinessRuleDto>> CreateBusinessRule(
            Guid programConfigurationId,
            [FromBody] CreateBusinessRuleRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var rule = await _configService.CreateBusinessRuleAsync(
                    tenantId, programConfigurationId, request.Name,
                    request.RuleType, request.TriggerEvent,
                    request.Condition, request.Action);

                return Ok(new BusinessRuleDto
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    RuleType = rule.RuleType,
                    TriggerEvent = rule.TriggerEvent,
                    Priority = rule.Priority,
                    IsActive = rule.IsActive,
                    CreatedAt = rule.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all business rules for a program (ordered by priority).
        /// </summary>
        [HttpGet("{programConfigurationId}/rules")]
        public async Task<ActionResult<List<BusinessRuleDto>>> GetProgramRules(Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var rules = await _configService.GetProgramRulesAsync(tenantId, programConfigurationId);

                var ruleDtos = new List<BusinessRuleDto>();
                foreach (var rule in rules)
                {
                    ruleDtos.Add(new BusinessRuleDto
                    {
                        Id = rule.Id,
                        Name = rule.Name,
                        RuleType = rule.RuleType,
                        TriggerEvent = rule.TriggerEvent,
                        Priority = rule.Priority,
                        IsActive = rule.IsActive,
                        CreatedAt = rule.CreatedAt
                    });
                }

                return Ok(ruleDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== CONTENT TEMPLATES ====================

        /// <summary>
        /// Create a reusable content template (email, SMS, notification, etc).
        /// Supports variable substitution like {{patientName}}, {{stageName}}, etc.
        /// </summary>
        [HttpPost("{programConfigurationId}/templates")]
        public async Task<ActionResult<ContentTemplateDto>> CreateContentTemplate(
            Guid programConfigurationId,
            [FromBody] CreateContentTemplateRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var template = await _configService.CreateContentTemplateAsync(
                    tenantId, programConfigurationId, request.Name,
                    request.ContentType, request.Content, request.Subject);

                return Ok(new ContentTemplateDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    ContentType = template.ContentType,
                    Subject = template.Subject,
                    IsActive = template.IsActive,
                    CreatedAt = template.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get content templates for a program.
        /// Optionally filter by content type (Email, SMS, InAppNotification, etc).
        /// </summary>
        [HttpGet("{programConfigurationId}/templates")]
        public async Task<ActionResult<List<ContentTemplateDto>>> GetProgramTemplates(
            Guid programConfigurationId,
            [FromQuery] string? contentType = null)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var templates = await _configService.GetProgramTemplatesAsync(
                    tenantId, programConfigurationId, contentType);

                var templateDtos = new List<ContentTemplateDto>();
                foreach (var template in templates)
                {
                    templateDtos.Add(new ContentTemplateDto
                    {
                        Id = template.Id,
                        Name = template.Name,
                        ContentType = template.ContentType,
                        Subject = template.Subject,
                        IsActive = template.IsActive,
                        CreatedAt = template.CreatedAt
                    });
                }

                return Ok(templateDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== ROLE PERMISSIONS ====================

        /// <summary>
        /// Define a role and its permissions within a program workflow.
        /// Example roles: Doctor, Nurse, CareCoordinator, Admin
        /// </summary>
        [HttpPost("{programConfigurationId}/roles")]
        public async Task<ActionResult<RolePermissionDto>> CreateRolePermission(
            Guid programConfigurationId,
            [FromBody] CreateRolePermissionRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var role = await _configService.CreateRolePermissionAsync(
                    tenantId, programConfigurationId, request.RoleName,
                    request.AccessibleStages, request.Permissions);

                return Ok(new RolePermissionDto
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    AccessibleStages = role.AccessibleStages,
                    Permissions = role.Permissions,
                    CanViewPathways = role.CanViewPathways,
                    CanApproveTransitions = role.CanApproveTransitions,
                    IsActive = role.IsActive,
                    CreatedAt = role.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all roles for a program.
        /// </summary>
        [HttpGet("{programConfigurationId}/roles")]
        public async Task<ActionResult<List<RolePermissionDto>>> GetProgramRoles(Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var roles = await _configService.GetProgramRolesAsync(tenantId, programConfigurationId);

                var roleDtos = new List<RolePermissionDto>();
                foreach (var role in roles)
                {
                    roleDtos.Add(new RolePermissionDto
                    {
                        Id = role.Id,
                        RoleName = role.RoleName,
                        AccessibleStages = role.AccessibleStages,
                        Permissions = role.Permissions,
                        CanViewPathways = role.CanViewPathways,
                        CanApproveTransitions = role.CanApproveTransitions,
                        IsActive = role.IsActive,
                        CreatedAt = role.CreatedAt
                    });
                }

                return Ok(roleDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== BULK OPERATIONS ====================

        /// <summary>
        /// Export a full program configuration (all stages, forms, rules, roles, templates) as JSON.
        /// Useful for sharing configs between tenants or version control.
        /// </summary>
        [HttpGet("{programConfigurationId}/export")]
        public async Task<ActionResult<string>> ExportConfiguration(Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var json = await _configService.ExportConfigurationAsync(tenantId, programConfigurationId);
                return Ok(new { configuration = json });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Import/clone a program configuration from JSON.
        /// Useful for onboarding new customers with pre-built workflows.
        /// </summary>
        [HttpPost("{programId}/import")]
        public async Task<ActionResult<ProgramConfigurationDto>> ImportConfiguration(
            Guid programId,
            [FromBody] ImportConfigurationRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var config = await _configService.ImportConfigurationAsync(
                    tenantId, programId, request.ConfigurationJson);

                return Ok(new ProgramConfigurationDto
                {
                    Id = config.Id,
                    ProgramId = config.ProgramId,
                    WorkflowType = config.WorkflowType,
                    IsActive = config.IsActive,
                    CreatedAt = config.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
