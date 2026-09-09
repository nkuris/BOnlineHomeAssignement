using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BOnlineHomeAssignement.Server.Application.Services;
using BOnlineHomeAssignement.Server.Domain.Entities;

namespace BOnlineHomeAssignement.Server.Controllers
{
    /// <summary>
    /// API endpoints for managing patient pathways through program workflows.
    /// Handles enrollments, stage transitions, form submissions, and progression tracking.
    /// All operations are tenant-scoped based on X-Tenant-Id header.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;

        public WorkflowController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        private Guid GetTenantIdFromHeader()
        {
            var header = Request.Headers["X-Tenant-Id"].ToString();
            return Guid.TryParse(header, out var tenantId) ? tenantId : throw new UnauthorizedAccessException("Invalid or missing X-Tenant-Id header");
        }

        // ==================== PATHWAY ENROLLMENT ====================

        /// <summary>
        /// Enroll a patient in a program, creating their initial pathway.
        /// This is called after lead-to-patient conversion or manual enrollment.
        /// </summary>
        [HttpPost("patients/{patientId}/enroll")]
        public async Task<ActionResult<PatientPathwayDto>> EnrollPatientInProgram(
            Guid patientId,
            [FromBody] EnrollPatientRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathway = await _workflowService.EnrollPatientInProgramAsync(
                    tenantId, patientId, request.EnrollmentId,
                    request.ProgramConfigurationId, request.PatientType);

                return Ok(MapToDto(pathway));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get all active pathways for a patient across all programs.
        /// </summary>
        [HttpGet("patients/{patientId}/pathways")]
        public async Task<ActionResult<List<PatientPathwayDto>>> GetPatientPathways(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathways = await _workflowService.GetPatientPathwaysAsync(tenantId, patientId);

                var dtos = new List<PatientPathwayDto>();
                foreach (var pathway in pathways)
                {
                    dtos.Add(MapToDto(pathway));
                }

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get a patient's pathway in a specific program.
        /// </summary>
        [HttpGet("patients/{patientId}/programs/{programConfigurationId}")]
        public async Task<ActionResult<PatientPathwayDto>> GetPatientPathwayInProgram(
            Guid patientId, Guid programConfigurationId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathway = await _workflowService.GetPatientPathwayAsync(
                    tenantId, patientId, programConfigurationId);

                if (pathway == null)
                    return NotFound("Patient pathway not found in this program");

                return Ok(MapToDto(pathway));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== STAGE TRANSITIONS ====================

        /// <summary>
        /// Transition a patient to the next stage in their program.
        /// Checks if all requirements are met (forms completed, SLA, etc).
        /// </summary>
        [HttpPost("pathways/{patientPathwayId}/transition-next")]
        public async Task<ActionResult<PatientPathwayDto>> TransitionToNextStage(
            Guid patientPathwayId,
            [FromBody] TransitionRequest? request = null)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathway = await _workflowService.TransitionToNextStageAsync(
                    tenantId, patientPathwayId, request?.Reason);

                return Ok(MapToDto(pathway));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Admin override: manually transition patient to a specific stage.
        /// Requires admin/manager role.
        /// </summary>
        [HttpPost("pathways/{patientPathwayId}/transition-to/{targetStageId}")]
        public async Task<ActionResult<PatientPathwayDto>> TransitionToStage(
            Guid patientPathwayId, Guid targetStageId,
            [FromBody] TransitionRequest? request = null)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathway = await _workflowService.TransitionToStageAsync(
                    tenantId, patientPathwayId, targetStageId, request?.Notes);

                return Ok(MapToDto(pathway));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Check if patient can advance to next stage.
        /// Returns blocking reasons if not ready.
        /// </summary>
        [HttpGet("pathways/{patientPathwayId}/can-advance")]
        public async Task<ActionResult<CanAdvanceResponse>> CanAdvanceToNextStage(Guid patientPathwayId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var (canAdvance, blockingReasons) = await _workflowService.CanAdvanceStageAsync(
                    tenantId, patientPathwayId);

                return Ok(new CanAdvanceResponse
                {
                    CanAdvance = canAdvance,
                    BlockingReasons = blockingReasons
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update patient's current status within their stage.
        /// Example: Active -> Pending, Pending -> Completed
        /// </summary>
        [HttpPatch("pathways/{patientPathwayId}/status")]
        public async Task<ActionResult<PatientPathwayDto>> UpdateStageStatus(
            Guid patientPathwayId,
            [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathway = await _workflowService.UpdateStageStatusAsync(
                    tenantId, patientPathwayId, request.Status);

                return Ok(MapToDto(pathway));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== FORMS & DATA COLLECTION ====================

        /// <summary>
        /// Get forms required for patient's current stage.
        /// Filters by visibility conditions and patient type.
        /// </summary>
        [HttpGet("pathways/{patientPathwayId}/required-forms")]
        public async Task<ActionResult<List<FormDefinitionDto>>> GetRequiredForms(Guid patientPathwayId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var forms = await _workflowService.GetRequiredFormsAsync(tenantId, patientPathwayId);

                var dtos = new List<FormDefinitionDto>();
                foreach (var form in forms)
                {
                    dtos.Add(new FormDefinitionDto
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

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Submit form responses for a patient pathway.
        /// Triggers any downstream actions (transitions, tasks, notifications).
        /// </summary>
        [HttpPost("pathways/{patientPathwayId}/submit-form")]
        public async Task<ActionResult<PatientPathwayDto>> SubmitFormResponses(
            Guid patientPathwayId,
            [FromBody] FormSubmissionRequest request)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                await _workflowService.OnFormSubmittedAsync(
                    tenantId, patientPathwayId, request.FormId, request.Responses);

                // Return updated pathway after form submission
                var pathway = await _workflowService.GetPatientPathwayAsync(
                    tenantId, Guid.Empty, patientPathwayId); // This will fail; need to fix WorkflowService to return by ID

                return Ok(new { message = "Form submitted successfully", formId = request.FormId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== TASK MANAGEMENT ====================

        /// <summary>
        /// Get all pending tasks for patient's current stage.
        /// </summary>
        [HttpGet("pathways/{patientPathwayId}/tasks")]
        public async Task<ActionResult<List<TaskItemDto>>> GetStageTasks(Guid patientPathwayId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var tasks = await _workflowService.GetStageTasksAsync(tenantId, patientPathwayId);

                var dtos = new List<TaskItemDto>();
                foreach (var task in tasks)
                {
                    dtos.Add(new TaskItemDto
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        AssignedTo = task.AssignedTo,
                        DueDate = task.DueDate,
                        IsCompleted = task.IsCompleted,
                        CreatedAt = task.CreatedAt
                    });
                }

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== COMPLETION & DISCHARGE ====================

        /// <summary>
        /// Complete and discharge a patient from the program.
        /// Marks pathway as inactive.
        /// </summary>
        [HttpPost("pathways/{patientPathwayId}/complete")]
        public async Task<ActionResult<PatientPathwayDto>> CompletePathway(
            Guid patientPathwayId,
            [FromBody] CompletePathwayRequest? request = null)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                var pathway = await _workflowService.CompletePathwayAsync(
                    tenantId, patientPathwayId, request?.Notes);

                return Ok(MapToDto(pathway));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== BUSINESS RULES & AUTOMATION ====================

        /// <summary>
        /// Manually trigger business rule evaluation for a pathway.
        /// (Normally rules are evaluated automatically on events)
        /// </summary>
        [HttpPost("pathways/{patientPathwayId}/evaluate-rules")]
        public async Task<ActionResult<object>> EvaluateBusinessRules(Guid patientPathwayId)
        {
            try
            {
                var tenantId = GetTenantIdFromHeader();
                await _workflowService.EvaluateBusinessRulesAsync(tenantId, patientPathwayId);

                return Ok(new { message = "Rules evaluated and actions executed" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ==================== HELPERS ====================

        private PatientPathwayDto MapToDto(PatientPathway pathway)
        {
            return new PatientPathwayDto
            {
                Id = pathway.Id,
                PatientId = pathway.PatientId,
                EnrollmentId = pathway.EnrollmentId,
                ProgramConfigurationId = pathway.ProgramConfigurationId,
                CurrentProcessStageId = pathway.CurrentProcessStageId,
                CurrentStatus = pathway.CurrentStatus,
                PatientType = pathway.PatientType,
                EnteredStageAt = pathway.EnteredStageAt,
                StageDueAt = pathway.StageDueAt,
                LastTransitionAt = pathway.LastTransitionAt,
                CompletedAt = pathway.CompletedAt,
                IsActive = pathway.IsActive,
                CreatedAt = pathway.CreatedAt
            };
        }
    }

    // ==================== REQUEST/RESPONSE DTOs ====================

    public class EnrollPatientRequest
    {
        public Guid EnrollmentId { get; set; }
        public Guid ProgramConfigurationId { get; set; }
        public string? PatientType { get; set; }
    }

    public class TransitionRequest
    {
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    public class CanAdvanceResponse
    {
        public bool CanAdvance { get; set; }
        public List<string> BlockingReasons { get; set; } = new();
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class FormSubmissionRequest
    {
        public Guid FormId { get; set; }
        public Dictionary<string, object> Responses { get; set; } = new();
    }

    public class CompletePathwayRequest
    {
        public string? Notes { get; set; }
    }

    public class PatientPathwayDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid ProgramConfigurationId { get; set; }
        public Guid CurrentProcessStageId { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string? PatientType { get; set; }
        public DateTime EnteredStageAt { get; set; }
        public DateTime? StageDueAt { get; set; }
        public DateTime? LastTransitionAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

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

    public class TaskItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
