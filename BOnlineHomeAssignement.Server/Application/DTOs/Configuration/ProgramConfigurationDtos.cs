using System;
using System.Collections.Generic;

namespace BOnlineHomeAssignement.Server.Application.DTOs
{
    // ==================== PROGRAM CONFIGURATION DTOs ====================

    public class ProgramConfigurationDto
    {
        public Guid Id { get; set; }
        public Guid ProgramId { get; set; }
        public string WorkflowType { get; set; } = "Linear";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProgramConfigurationRequest
    {
        public Guid ProgramId { get; set; }
        public string? WorkflowType { get; set; } = "Linear";
    }

    public class ImportConfigurationRequest
    {
        public string ConfigurationJson { get; set; } = string.Empty;
    }

    // ==================== PROCESS STAGE DTOs ====================

    public class ProcessStageDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public int? DurationDays { get; set; }
        public string? Description { get; set; }
        public string DefaultStatus { get; set; } = "Active";
        public string? AllowedStatuses { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProcessStageRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public int? DurationDays { get; set; }
        public string? Description { get; set; }
    }

    // ==================== FORM DEFINITION DTOs ====================

    public class FormDefinitionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FieldsDefinition { get; set; } = string.Empty;
        public string? ApplicableStages { get; set; }
        public bool IsRequired { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateFormDefinitionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string FieldsDefinition { get; set; } = "[]";
        public string? ApplicableStages { get; set; }
    }

    // ==================== BUSINESS RULE DTOs ====================

    public class BusinessRuleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RuleType { get; set; } = "StageTransition";
        public string TriggerEvent { get; set; } = "Automatic";
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateBusinessRuleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string RuleType { get; set; } = "StageTransition";
        public string TriggerEvent { get; set; } = "Automatic";
        public string Condition { get; set; } = "{}";
        public string Action { get; set; } = "{}";
    }

    // ==================== CONTENT TEMPLATE DTOs ====================

    public class ContentTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = "Email";
        public string? Subject { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateContentTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = "Email"; // Email, SMS, InAppNotification, FormText, Letter
        public string Content { get; set; } = string.Empty;
        public string? Subject { get; set; }
    }

    // ==================== ROLE PERMISSION DTOs ====================

    public class RolePermissionDto
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? AccessibleStages { get; set; }
        public string? Permissions { get; set; }
        public bool CanViewPathways { get; set; }
        public bool CanApproveTransitions { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRolePermissionRequest
    {
        public string RoleName { get; set; } = string.Empty;
        public string? AccessibleStages { get; set; }
        public string? Permissions { get; set; } // CSV: Read, Create, Edit, Delete, Approve, Escalate
    }

    // ==================== TASK DEFINITION DTOs ====================

    public class TaskDefinitionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TaskType { get; set; } = "Manual"; // Manual, Automated, Notification
        public int? DueDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTaskDefinitionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TaskType { get; set; } = "Manual";
        public int DueDays { get; set; } = 7;
    }
}
