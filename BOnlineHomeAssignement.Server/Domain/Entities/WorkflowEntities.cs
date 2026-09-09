using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    /// <summary>
    /// Defines the configuration and workflow for a care program.
    /// Each tenant can have multiple programs with different stages and workflows.
    /// </summary>
    public class ProgramConfiguration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramId { get; set; }
        public Program? Program { get; set; }

        /// <summary>Workflow type: Linear, Branching, or Custom</summary>
        [Required]
        [MaxLength(50)]
        public string WorkflowType { get; set; } = "Linear"; // Linear, Branching, Custom

        /// <summary>JSON-serialized workflow definition with stages and transitions</summary>
        public string? WorkflowDefinition { get; set; }

        /// <summary>JSON-serialized default business rules for this program</summary>
        public string? DefaultBusinessRules { get; set; }

        /// <summary>Patient types supported in this program (e.g., "Initial", "Advanced", "Chronic")</summary>
        [MaxLength(500)]
        public string? PatientTypes { get; set; } // CSV or JSON

        /// <summary>Whether this program is active</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public List<ProcessStage> ProcessStages { get; set; } = new();
        public List<PatientPathway> PatientPathways { get; set; } = new();
        public List<FormDefinition> FormDefinitions { get; set; } = new();
        public List<TaskDefinition> TaskDefinitions { get; set; } = new();
        public List<ContentTemplate> ContentTemplates { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
    }

    /// <summary>
    /// Defines a stage/step in a program workflow.
    /// Example stages: "Initial Assessment", "Treatment Planning", "Follow-up", "Discharge"
    /// </summary>
    public class ProcessStage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        /// <summary>Stage name/title</summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Stage order in workflow (1, 2, 3, ...)</summary>
        public int Order { get; set; }

        /// <summary>Description of what happens at this stage</summary>
        public string? Description { get; set; }

        /// <summary>Duration in days (SLA for completing this stage)</summary>
        public int? DurationDays { get; set; }

        /// <summary>Possible statuses within this stage</summary>
        [MaxLength(500)]
        public string? AllowedStatuses { get; set; } // CSV: "Active,Pending,Completed,Skipped"

        /// <summary>Default status when entering this stage</summary>
        [MaxLength(50)]
        public string DefaultStatus { get; set; } = "Active";

        /// <summary>JSON: which roles can perform actions at this stage</summary>
        public string? RoleRequirements { get; set; }

        /// <summary>FormDefinition IDs required at this stage</summary>
        public string? RequiredFormIds { get; set; } // CSV of Guid

        /// <summary>TaskDefinition IDs automatically triggered at this stage</summary>
        public string? TriggeredTaskIds { get; set; } // CSV of Guid

        /// <summary>Conditions to auto-advance to next stage (JSON)</summary>
        public string? AutoAdvanceConditions { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<PatientPathway> PatientPathways { get; set; } = new();
    }

    /// <summary>
    /// Tracks a patient's progression through a program workflow.
    /// Links patient to their current stage and pathway.
    /// </summary>
    public class PatientPathway
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        public Guid EnrollmentId { get; set; }
        public Enrollment? Enrollment { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        /// <summary>Current stage the patient is in</summary>
        [Required]
        public Guid CurrentProcessStageId { get; set; }
        public ProcessStage? CurrentProcessStage { get; set; }

        /// <summary>Current status within the stage</summary>
        [MaxLength(50)]
        public string CurrentStatus { get; set; } = "Active";

        /// <summary>Patient type/cohort for this pathway</summary>
        [MaxLength(100)]
        public string? PatientType { get; set; } // "Initial", "Advanced", "Chronic", etc.

        /// <summary>Custom data collected via responses/forms</summary>
        public string? PathwayData { get; set; } // JSON storing form responses, demographics, etc.

        /// <summary>When the patient entered current stage</summary>
        public DateTime EnteredStageAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the patient should complete current stage (SLA)</summary>
        public DateTime? StageDueAt { get; set; }

        /// <summary>Last time pathway was updated</summary>
        public DateTime? LastTransitionAt { get; set; }

        /// <summary>When patient completed the program</summary>
        public DateTime? CompletedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Audit
        public string? TransitionNotes { get; set; }
    }

    /// <summary>
    /// Defines a form or questionnaire to be completed at a stage.
    /// Forms collect patient data, responses, and drive workflow decisions.
    /// </summary>
    public class FormDefinition
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>JSON-serialized form fields</summary>
        [Required]
        public string FieldsDefinition { get; set; } = "[]"; // Array of field definitions

        /// <summary>Which stages this form appears in</summary>
        public string? ApplicableStages { get; set; } // CSV of ProcessStage IDs or names

        /// <summary>Conditions when this form is shown (e.g., "if patientType == 'Advanced'")</summary>
        public string? VisibilityConditions { get; set; } // JSON

        /// <summary>Upon form submission, which actions trigger</summary>
        public string? OnSubmitActions { get; set; } // JSON: { "transitions": [...], "tasks": [...] }

        public bool IsRequired { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Defines an automated or manual task to be performed by staff.
    /// Tasks can be triggered by rules, stage entry, or form submission.
    /// </summary>
    public class TaskDefinition
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Task type: "Assessment", "Treatment", "FollowUp", "Notification", "Escalation", etc.</summary>
        [MaxLength(50)]
        public string TaskType { get; set; } = "Manual"; // Manual, Automated, Notification

        /// <summary>Who should perform this task</summary>
        [MaxLength(100)]
        public string? AssignToRole { get; set; } // e.g., "Nurse", "Doctor", "CareCoordinator"

        /// <summary>Days after stage entry when this task should be created</summary>
        public int OffsetDays { get; set; } = 0;

        /// <summary>How long to complete (in days)</summary>
        public int? DueDays { get; set; } = 7;

        /// <summary>If not completed by DueDays, create escalation task</summary>
        public bool AutoEscalate { get; set; } = false;

        /// <summary>Content template to use (for notifications, assessments)</summary>
        public Guid? ContentTemplateId { get; set; }
        public ContentTemplate? ContentTemplate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<TaskItem> CreatedTasks { get; set; } = new();
    }

    /// <summary>
    /// Reusable content templates for emails, SMS, notifications, and form text.
    /// Supports variable substitution (e.g., {{patientName}}, {{stageDate}})
    /// </summary>
    public class ContentTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Template type: "Email", "SMS", "InAppNotification", "FormText", "Letter"</summary>
        [MaxLength(50)]
        public string ContentType { get; set; } = "Email";

        /// <summary>Subject line (for email) or title</summary>
        [MaxLength(500)]
        public string? Subject { get; set; }

        /// <summary>Template content with {{variables}}</summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>Supported variables for this template</summary>
        [MaxLength(500)]
        public string? SupportedVariables { get; set; } // CSV: patientName, stageName, dueDate, etc.

        /// <summary>Language code (e.g., "en", "es", "he", "fr")</summary>
        [MaxLength(10)]
        public string Language { get; set; } = "en";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Defines if-then rules that drive workflow decisions and automations.
    /// Examples:
    /// - If patientType=="Chronic" AND stage=="Initial", show AdditionalQuestionnaire
    /// - If formScore > 70, auto-advance to next stage
    /// - If task overdue 3 days, escalate to manager
    /// </summary>
    public class BusinessRule
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Rule type: "StageTransition", "TaskTrigger", "FormVisibility", "Escalation", "Notification"</summary>
        [MaxLength(50)]
        public string RuleType { get; set; } = "StageTransition";

        /// <summary>When this rule evaluates (e.g., "OnFormSubmit", "OnStageEntry", "Daily", "OnDayX")</summary>
        [MaxLength(100)]
        public string TriggerEvent { get; set; } = "Automatic";

        /// <summary>The condition to evaluate (JSON-serialized logic)</summary>
        [Required]
        public string Condition { get; set; } = "{}"; // JSON: { "type": "and", "conditions": [...] }

        /// <summary>The action to perform if condition is TRUE (JSON)</summary>
        [Required]
        public string Action { get; set; } = "{}"; // JSON: { "type": "transition", "targetStage": "...", ... }

        /// <summary>Priority (1=highest, affects evaluation order)</summary>
        public int Priority { get; set; } = 5;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Defines roles and their permissions within a program workflow.
    /// Allows customer to specify who can do what at each stage.
    /// </summary>
    public class RolePermission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; } = string.Empty; // e.g., "Doctor", "Nurse", "CareCoordinator"

        /// <summary>Which process stages this role can access</summary>
        [MaxLength(500)]
        public string? AccessibleStages { get; set; } // CSV of stage names or IDs

        /// <summary>Permissions as CSV: "Read", "Create", "Edit", "Delete", "Approve", "Escalate"</summary>
        [MaxLength(500)]
        public string? Permissions { get; set; }

        /// <summary>Which task types this role can perform</summary>
        [MaxLength(500)]
        public string? AssignableTaskTypes { get; set; }

        /// <summary>Can view patient pathways (for this tenant)</summary>
        public bool CanViewPathways { get; set; } = true;

        /// <summary>Can approve stage transitions</summary>
        public bool CanApproveTransitions { get; set; } = false;

        /// <summary>Can override rules/skip stages</summary>
        public bool CanOverrideRules { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a timeline event in a patient's workflow journey.
    /// Events are recorded for every significant action: enrollment, stage transition, form submission, task completion, etc.
    /// VisibleToPatient controls which events are shown to the patient (clinical events may be hidden).
    /// </summary>
    public class WorkflowEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid PatientPathwayId { get; set; }
        public PatientPathway? PatientPathway { get; set; }

        /// <summary>
        /// Event type: "Enrolled", "StageTransitioned", "FormSubmitted", "TaskCreated", "TaskCompleted", 
        /// "QuestionnaireScheduled", "QuestionnaireSubmitted", "TriggerFired", "NurseAssigned", etc.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable event description for timeline display
        /// </summary>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Who/what triggered the event: "System", "User:{UserId}", "Trigger:{TriggerId}", etc.
        /// </summary>
        [MaxLength(200)]
        public string? TriggeredBy { get; set; }

        /// <summary>
        /// Additional event data as JSON (stage name, form submitted fields, task ID, etc.)
        /// </summary>
        public string? EventData { get; set; }

        /// <summary>
        /// Whether this event should be visible to the patient in their timeline
        /// </summary>
        public bool VisibleToPatient { get; set; } = true;

        /// <summary>
        /// When the event occurred
        /// </summary>
        [Required]
        public DateTime EventOccurredAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a questionnaire/survey to be sent to a patient at configured times.
    /// Supports recurrence patterns (once, weekly, monthly, specific days after enrollment).
    /// </summary>
    public class Questionnaire
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProgramConfigurationId { get; set; }
        public ProgramConfiguration? ProgramConfiguration { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Questionnaire fields as JSON array (similar to FormDefinition.FieldsDefinition)
        /// </summary>
        public string? FieldsDefinition { get; set; }

        /// <summary>
        /// Recurrence type: "Once", "Weekly", "Monthly", "DaysAfterEnrollment", "OnEvent"
        /// </summary>
        [MaxLength(50)]
        public string RecurrenceType { get; set; } = "Once";

        /// <summary>
        /// Recurrence value: for "DaysAfterEnrollment" = number of days; for "Weekly" = day of week; etc.
        /// </summary>
        public string? RecurrenceValue { get; set; }

        /// <summary>
        /// If OnEvent, which event type triggers this questionnaire: "StageTransitioned", "FormSubmitted", etc.
        /// </summary>
        [MaxLength(100)]
        public string? TriggerEventType { get; set; }

        /// <summary>
        /// Maximum number of times this questionnaire should be sent (null = unlimited)
        /// </summary>
        public int? MaxSendCount { get; set; }

        /// <summary>
        /// Whether this questionnaire is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public List<QuestionnaireResponse> Responses { get; set; } = new();
    }

    /// <summary>
    /// Tracks when a patient answers a questionnaire.
    /// Linked to the specific questionnaire and patient pathway.
    /// </summary>
    public class QuestionnaireResponse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid QuestionnaireId { get; set; }
        public Questionnaire? Questionnaire { get; set; }

        [Required]
        public Guid PatientPathwayId { get; set; }
        public PatientPathway? PatientPathway { get; set; }

        /// <summary>
        /// Patient's answers as JSON (matched to field names from Questionnaire.FieldsDefinition)
        /// </summary>
        [Required]
        public string ResponseData { get; set; } = string.Empty;

        /// <summary>
        /// When the questionnaire was sent to the patient
        /// </summary>
        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the patient completed and submitted the questionnaire
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Response status: "Pending", "Completed", "Expired", "Cancelled"
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Tracks execution of automated triggers (time-based or event-based).
    /// Records attempts, retries, failures, and prevents duplicate executions.
    /// </summary>
    public class TriggerExecution
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid PatientPathwayId { get; set; }
        public PatientPathway? PatientPathway { get; set; }

        /// <summary>
        /// Trigger type: "TimeBasedFollowUp", "EventBasedFormResponse", "QuestionnaireSchedule", etc.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// Trigger definition/configuration ID (from business rules or questionnaire setup)
        /// </summary>
        public string? TriggerConfigId { get; set; }

        /// <summary>
        /// Execution status: "Pending", "Processing", "Succeeded", "Failed", "Cancelled"
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Number of execution attempts made
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        /// <summary>
        /// Maximum retry attempts allowed
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// When to retry next (null if no retry planned)
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// Unique key to prevent duplicate executions on the same day/occurrence
        /// Format: "{TriggerType}_{PatientPathwayId}_{Date}"
        /// </summary>
        [MaxLength(250)]
        public string? DeduplicationKey { get; set; }

        /// <summary>
        /// Result/outcome if execution completed: "TaskCreated", "QuestionnaireScheduled", "FormValidationFailed", etc.
        /// </summary>
        [MaxLength(200)]
        public string? ExecutionResult { get; set; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// When execution was scheduled
        /// </summary>
        [Required]
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When execution actually started (null until attempted)
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When execution completed (null if still pending/failed)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a communication message queued for sending to a patient.
    /// Abstracted to support multiple channels: Email, SMS, WhatsApp, Voice.
    /// Provider pattern allows pluggable implementations per channel.
    /// </summary>
    public class CommunicationMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TenantId { get; set; }

        [Required]
        public Guid PatientPathwayId { get; set; }
        public PatientPathway? PatientPathway { get; set; }

        /// <summary>
        /// Channel: "Email", "SMS", "WhatsApp", "Voice", "InApp"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// Recipient contact info (email address, phone number, etc.)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Message subject (for Email)
        /// </summary>
        [MaxLength(200)]
        public string? Subject { get; set; }

        /// <summary>
        /// Message body/content
        /// </summary>
        [Required]
        public string MessageBody { get; set; } = string.Empty;

        /// <summary>
        /// Message type/category: "Appointment", "Questionnaire", "Reminder", "Alert", "FollowUp", "FormSubmitted", etc.
        /// </summary>
        [MaxLength(50)]
        public string? MessageType { get; set; }

        /// <summary>
        /// Message status: "Queued", "Sent", "Delivered", "Failed", "Bounced"
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Queued";

        /// <summary>
        /// Number of send attempts made
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        /// <summary>
        /// Maximum retry attempts allowed
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// External provider response/tracking ID (e.g., from SendGrid, Twilio)
        /// </summary>
        [MaxLength(200)]
        public string? ProviderMessageId { get; set; }

        /// <summary>
        /// Error/failure reason if applicable
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// When the message should be sent
        /// </summary>
        [Required]
        public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the message was actually sent
        /// </summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// When the message was delivered (if tracking available)
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Related entity ID (task ID, questionnaire ID, form submission ID, etc.)
        /// </summary>
        public string? RelatedEntityId { get; set; }

        /// <summary>
        /// Type of related entity for auditing
        /// </summary>
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
