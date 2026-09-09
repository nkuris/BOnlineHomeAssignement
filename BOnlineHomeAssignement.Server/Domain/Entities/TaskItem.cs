using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    /// <summary>
    /// Represents a task in a patient's workflow.
    /// Tasks can be created manually or automatically from triggers/questionnaires.
    /// Each task tracks patient/enrollment/program linkage, status, priority, and lifecycle.
    /// </summary>
    public class TaskItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant ID for multi-tenant isolation
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Foreign key to Patient
        /// </summary>
        [Required]
        public Guid PatientId { get; set; }
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        /// <summary>
        /// Foreign key to Enrollment (which enrollment this task belongs to)
        /// </summary>
        public Guid? EnrollmentId { get; set; }
        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment? Enrollment { get; set; }

        /// <summary>
        /// Foreign key to Program (which program this task is part of)
        /// </summary>
        public Guid? ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))]
        public Program? Program { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// User ID this task is assigned to (can be Nurse, Admin, etc.)
        /// </summary>
        [MaxLength(200)]
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Task type: "FollowUp", "Assessment", "Questionnaire", "Appointment", "Reminder", "Manual", etc.
        /// </summary>
        [MaxLength(50)]
        public string TaskType { get; set; } = "Manual";

        /// <summary>
        /// Task status: "Pending", "InProgress", "Completed", "Cancelled", "Overdue"
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Priority level: "Low", "Medium", "High", "Critical"
        /// </summary>
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// When task is due
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Source of task creation: "Manual" (user created) or "Automatic" (trigger/form/questionnaire)
        /// </summary>
        [MaxLength(50)]
        public string CreationSource { get; set; } = "Manual";

        /// <summary>
        /// If automatic, which entity triggered the task creation (e.g., "FormSubmitted", "TriggerFired", "QuestionnaireScheduled")
        /// </summary>
        public string? CreatedBySourceId { get; set; }

        /// <summary>
        /// Outcome of the task when completed: "Completed", "Cancelled", "Deferred", etc.
        /// </summary>
        [MaxLength(50)]
        public string? Outcome { get; set; }

        /// <summary>
        /// When the task was completed/cancelled
        /// </summary>
        public DateTime? CompletionDate { get; set; }

        /// <summary>
        /// Notes on completion
        /// </summary>
        public string? CompletionNotes { get; set; }

        /// <summary>
        /// Is task completed (derived from Status)
        /// </summary>
        public bool IsCompleted => Status == "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
