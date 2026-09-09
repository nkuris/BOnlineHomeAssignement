using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using DomainProgram = BOnlineHomeAssignement.Server.Domain.Entities.Program;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Service for managing patient pathways through configurable program workflows.
    /// Handles stage transitions, rule evaluation, and automated task creation.
    /// </summary>
    public interface IWorkflowService
    {
        /// <summary>
        /// Enroll a patient into a program configuration, creating initial pathway and stage entry.
        /// </summary>
        Task<PatientPathway> EnrollPatientInProgramAsync(
            Guid tenantId, Guid patientId, Guid enrollmentId, 
            Guid programConfigurationId, string? patientType = null);

        /// <summary>
        /// Transition patient from current stage to next stage based on rules/conditions.
        /// Returns the new PatientPathway state.
        /// </summary>
        Task<PatientPathway> TransitionToNextStageAsync(
            Guid tenantId, Guid patientPathwayId, string? reason = null);

        /// <summary>
        /// Manually transition patient to a specific stage (admin override).
        /// </summary>
        Task<PatientPathway> TransitionToStageAsync(
            Guid tenantId, Guid patientPathwayId, Guid targetStageId, string? notes = null);

        /// <summary>
        /// Update patient's current status within a stage (e.g., "Pending" -> "Completed").
        /// </summary>
        Task<PatientPathway> UpdateStageStatusAsync(
            Guid tenantId, Guid patientPathwayId, string newStatus);

        /// <summary>
        /// Get all active pathways for a patient across all programs.
        /// </summary>
        Task<List<PatientPathway>> GetPatientPathwaysAsync(Guid tenantId, Guid patientId);

        /// <summary>
        /// Get current pathway for a patient in a specific program.
        /// </summary>
        Task<PatientPathway?> GetPatientPathwayAsync(
            Guid tenantId, Guid patientId, Guid programConfigurationId);

        /// <summary>
        /// Check if patient can advance to next stage based on business rules.
        /// </summary>
        Task<(bool CanAdvance, List<string> BlockingReasons)> CanAdvanceStageAsync(
            Guid tenantId, Guid patientPathwayId);

        /// <summary>
        /// Evaluate all business rules for a pathway and execute matching actions.
        /// </summary>
        Task EvaluateBusinessRulesAsync(Guid tenantId, Guid patientPathwayId);

        /// <summary>
        /// Get forms required for patient's current stage.
        /// </summary>
        Task<List<FormDefinition>> GetRequiredFormsAsync(Guid tenantId, Guid patientPathwayId);

        /// <summary>
        /// Record form submission and trigger downstream actions (tasks, transitions, etc).
        /// </summary>
        Task OnFormSubmittedAsync(
            Guid tenantId, Guid patientPathwayId, Guid formId, Dictionary<string, object> responses);

        /// <summary>
        /// Get all tasks for a patient pathway in current stage.
        /// </summary>
        Task<List<TaskItem>> GetStageTasksAsync(Guid tenantId, Guid patientPathwayId);

        /// <summary>
        /// Complete a pathway (mark as completed/discharged).
        /// </summary>
        Task<PatientPathway> CompletePathwayAsync(Guid tenantId, Guid patientPathwayId, string? notes = null);
    }

    public class WorkflowService : IWorkflowService
    {
        private readonly IRepository<PatientPathway> _pathwayRepository;
        private readonly IRepository<ProgramConfiguration> _programConfigRepository;
        private readonly IRepository<ProcessStage> _processStageRepository;
        private readonly IRepository<FormDefinition> _formRepository;
        private readonly IRepository<BusinessRule> _ruleRepository;
        private readonly IRepository<TaskDefinition> _taskDefRepository;
        private readonly IRepository<TaskItem> _taskRepository;
        private readonly IRepository<Patient> _patientRepository;
        private readonly AppDbContext _dbContext;
        private readonly IRepository<WorkflowEvent> _eventRepository;
        private readonly TriggerService _triggerService;

        public WorkflowService(
            IRepository<PatientPathway> pathwayRepository,
            IRepository<ProgramConfiguration> programConfigRepository,
            IRepository<ProcessStage> processStageRepository,
            IRepository<FormDefinition> formRepository,
            IRepository<BusinessRule> ruleRepository,
            IRepository<TaskDefinition> taskDefRepository,
            IRepository<TaskItem> taskRepository,
            IRepository<Patient> patientRepository,
            AppDbContext dbContext,
            IRepository<WorkflowEvent> eventRepository,
            TriggerService triggerService)
        {
            _pathwayRepository = pathwayRepository;
            _programConfigRepository = programConfigRepository;
            _processStageRepository = processStageRepository;
            _formRepository = formRepository;
            _ruleRepository = ruleRepository;
            _taskDefRepository = taskDefRepository;
            _taskRepository = taskRepository;
            _patientRepository = patientRepository;
            _dbContext = dbContext;
            _eventRepository = eventRepository;
            _triggerService = triggerService;
        }

        /// <summary>
        /// Enroll patient in a program, creating initial pathway at first stage.
        /// </summary>
        public async Task<PatientPathway> EnrollPatientInProgramAsync(
            Guid tenantId, Guid patientId, Guid enrollmentId,
            Guid programConfigurationId, string? patientType = null)
        {
            // Get program config and first stage
            var programConfig = await _programConfigRepository.GetByIdAsync(programConfigurationId);
            if (programConfig == null || programConfig.TenantId != tenantId)
                throw new InvalidOperationException("Program configuration not found or unauthorized.");

            var firstStage = await _processStageRepository.FindAsync(
                ps => ps.ProgramConfigurationId == programConfigurationId
                   && ps.TenantId == tenantId
                   && ps.IsActive);

            if (!firstStage.Any())
                throw new InvalidOperationException("No stages defined for this program.");

            var initialStage = firstStage.OrderBy(ps => ps.Order).First();

            // Create pathway
            var pathway = new PatientPathway
            {
                TenantId = tenantId,
                PatientId = patientId,
                EnrollmentId = enrollmentId,
                ProgramConfigurationId = programConfigurationId,
                CurrentProcessStageId = initialStage.Id,
                CurrentStatus = initialStage.DefaultStatus,
                PatientType = patientType,
                EnteredStageAt = DateTime.UtcNow,
                StageDueAt = initialStage.DurationDays.HasValue
                    ? DateTime.UtcNow.AddDays(initialStage.DurationDays.Value)
                    : null,
                IsActive = true
            };

            await _pathwayRepository.AddAsync(pathway);
            await _pathwayRepository.SaveChangesAsync();

            // Emit enrollment event
            await EmitWorkflowEventAsync(
                tenantId,
                pathway.Id,
                "Enrolled",
                $"Patient {patientId} enrolled in program {programConfigurationId}",
                "System",
                new { programConfigurationId, patientType, stageId = initialStage.Id });

            // Schedule initial follow-up trigger (e.g., 7 days after enrollment)
            await _triggerService.ScheduleFollowUpTriggerAsync(
                tenantId,
                pathway.Id,
                "TimeBasedFollowUp_InitialCheckIn",
                delayInDays: 7);

            // Create initial tasks if defined
            await TriggerStageTasksAsync(tenantId, pathway.Id, initialStage);

            return pathway;
        }

        /// <summary>
        /// Transition patient to next stage if rules allow.
        /// </summary>
        public async Task<PatientPathway> TransitionToNextStageAsync(
            Guid tenantId, Guid patientPathwayId, string? reason = null)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                throw new InvalidOperationException("Pathway not found or unauthorized.");

            // Check if can advance
            var (canAdvance, blockingReasons) = await CanAdvanceStageAsync(tenantId, patientPathwayId);
            if (!canAdvance)
                throw new InvalidOperationException(
                    $"Cannot advance stage. Reasons: {string.Join("; ", blockingReasons)}");

            // Get all stages, find current index
            var allStages = (await _processStageRepository.FindAsync(
                ps => ps.ProgramConfigurationId == pathway.ProgramConfigurationId
                   && ps.TenantId == tenantId
                   && ps.IsActive)).OrderBy(ps => ps.Order).ToList();

            var currentIndex = allStages.FindIndex(s => s.Id == pathway.CurrentProcessStageId);
            if (currentIndex < 0 || currentIndex >= allStages.Count - 1)
            {
                // Already at last stage or no progression
                pathway.CurrentStatus = "Completed";
                pathway.CompletedAt = DateTime.UtcNow;
                await _pathwayRepository.UpdateAsync(pathway);
                await _pathwayRepository.SaveChangesAsync();
                return pathway;
            }

            // Transition to next stage
            var nextStage = allStages[currentIndex + 1];
            pathway.CurrentProcessStageId = nextStage.Id;
            pathway.CurrentStatus = nextStage.DefaultStatus;
            pathway.EnteredStageAt = DateTime.UtcNow;
            pathway.StageDueAt = nextStage.DurationDays.HasValue
                ? DateTime.UtcNow.AddDays(nextStage.DurationDays.Value)
                : null;
            pathway.LastTransitionAt = DateTime.UtcNow;
            pathway.TransitionNotes = reason ?? "Auto-advanced";

            await _pathwayRepository.UpdateAsync(pathway);
            await _pathwayRepository.SaveChangesAsync();

            // Emit stage transition event
            await EmitWorkflowEventAsync(
                tenantId,
                patientPathwayId,
                "StageTransitioned",
                $"Progressed from {allStages[currentIndex].Name} to {nextStage.Name}",
                "System",
                new { fromStageId = allStages[currentIndex].Id, toStageId = nextStage.Id, reason });

            // Trigger any tasks for new stage
            await TriggerStageTasksAsync(tenantId, patientPathwayId, nextStage);

            // Evaluate rules for new stage
            await EvaluateBusinessRulesAsync(tenantId, patientPathwayId);

            return pathway;
        }

        /// <summary>
        /// Manually transition to a specific stage (admin-level operation).
        /// </summary>
        public async Task<PatientPathway> TransitionToStageAsync(
            Guid tenantId, Guid patientPathwayId, Guid targetStageId, string? notes = null)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                throw new InvalidOperationException("Pathway not found or unauthorized.");

            var targetStage = await _processStageRepository.GetByIdAsync(targetStageId);
            if (targetStage == null || targetStage.TenantId != tenantId
                || targetStage.ProgramConfigurationId != pathway.ProgramConfigurationId)
                throw new InvalidOperationException("Target stage not found or not in this program.");

            pathway.CurrentProcessStageId = targetStageId;
            pathway.CurrentStatus = targetStage.DefaultStatus;
            pathway.EnteredStageAt = DateTime.UtcNow;
            pathway.StageDueAt = targetStage.DurationDays.HasValue
                ? DateTime.UtcNow.AddDays(targetStage.DurationDays.Value)
                : null;
            pathway.LastTransitionAt = DateTime.UtcNow;
            pathway.TransitionNotes = notes ?? "Manual override";

            await _pathwayRepository.UpdateAsync(pathway);
            await _pathwayRepository.SaveChangesAsync();

            await TriggerStageTasksAsync(tenantId, patientPathwayId, targetStage);
            await EvaluateBusinessRulesAsync(tenantId, patientPathwayId);

            return pathway;
        }

        /// <summary>
        /// Update status within current stage.
        /// </summary>
        public async Task<PatientPathway> UpdateStageStatusAsync(
            Guid tenantId, Guid patientPathwayId, string newStatus)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                throw new InvalidOperationException("Pathway not found.");

            var currentStage = await _processStageRepository.GetByIdAsync(pathway.CurrentProcessStageId);
            if (currentStage == null)
                throw new InvalidOperationException("Current stage not found.");

            // Validate status is allowed in this stage
            if (!ValidateStatusForStage(currentStage, newStatus))
                throw new InvalidOperationException(
                    $"Status '{newStatus}' not allowed in stage '{currentStage.Name}'");

            pathway.CurrentStatus = newStatus;
            pathway.LastTransitionAt = DateTime.UtcNow;

            await _pathwayRepository.UpdateAsync(pathway);
            await _pathwayRepository.SaveChangesAsync();

            // Check if status change triggers rules
            await EvaluateBusinessRulesAsync(tenantId, patientPathwayId);

            return pathway;
        }

        /// <summary>
        /// Get all active pathways for a patient.
        /// </summary>
        public async Task<List<PatientPathway>> GetPatientPathwaysAsync(Guid tenantId, Guid patientId)
        {
            var results = await _pathwayRepository.FindAsync(
                pp => pp.TenantId == tenantId && pp.PatientId == patientId && pp.IsActive);
            return results.ToList();
        }

        /// <summary>
        /// Get patient's pathway in a specific program.
        /// </summary>
        public async Task<PatientPathway?> GetPatientPathwayAsync(
            Guid tenantId, Guid patientId, Guid programConfigurationId)
        {
            var results = await _pathwayRepository.FindAsync(
                pp => pp.TenantId == tenantId
                   && pp.PatientId == patientId
                   && pp.ProgramConfigurationId == programConfigurationId
                   && pp.IsActive);

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Check if patient can advance to next stage.
        /// </summary>
        public async Task<(bool CanAdvance, List<string> BlockingReasons)> CanAdvanceStageAsync(
            Guid tenantId, Guid patientPathwayId)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                return (false, new List<string> { "Pathway not found" });

            var blockingReasons = new List<string>();

            // Check if all required forms are completed
            var requiredForms = await GetRequiredFormsAsync(tenantId, patientPathwayId);
            // In production, parse PathwayData JSON to count completed forms
            // For now, simplified check
            if (requiredForms.Count > 0)
                blockingReasons.Add("Forms pending completion");

            // Check SLA
            if (pathway.StageDueAt.HasValue && DateTime.UtcNow > pathway.StageDueAt)
                blockingReasons.Add("Stage SLA exceeded");

            return (blockingReasons.Count == 0, blockingReasons);
        }

        /// <summary>
        /// Evaluate and execute all applicable business rules for the pathway.
        /// </summary>
        public async Task EvaluateBusinessRulesAsync(Guid tenantId, Guid patientPathwayId)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                return;

            var rules = (await _ruleRepository.FindAsync(
                br => br.TenantId == tenantId
                   && br.ProgramConfigurationId == pathway.ProgramConfigurationId
                   && br.IsActive)).OrderBy(br => br.Priority).ToList();

            foreach (var rule in rules)
            {
                try
                {
                    // Evaluate condition (simplified - in production use a rules engine)
                    if (EvaluateCondition(rule.Condition, pathway))
                    {
                        await ExecuteRuleAction(tenantId, patientPathwayId, rule.Action);
                    }
                }
                catch (Exception ex)
                {
                    // Log rule evaluation failure but continue with other rules
                    System.Diagnostics.Debug.WriteLine($"Rule evaluation failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get forms required at current stage, filtered by visibility conditions.
        /// </summary>
        public async Task<List<FormDefinition>> GetRequiredFormsAsync(Guid tenantId, Guid patientPathwayId)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                return new List<FormDefinition>();

            var forms = await _formRepository.FindAsync(
                f => f.TenantId == tenantId
                   && f.ProgramConfigurationId == pathway.ProgramConfigurationId
                   && f.IsActive
                   && f.IsRequired);

            // Filter by visibility conditions and applicable stages
            return forms.Where(f =>
            {
                // Check if form applies to current stage
                if (!string.IsNullOrEmpty(f.ApplicableStages))
                {
                    var stageNames = f.ApplicableStages.Split(',');
                    // Match against current stage (simplified)
                    // In production, parse stage and compare
                }

                // Check visibility conditions based on patient pathway data
                if (!string.IsNullOrEmpty(f.VisibilityConditions))
                {
                    if (!EvaluateVisibilityCondition(f.VisibilityConditions, pathway))
                        return false;
                }

                return true;
            }).ToList();
        }

        /// <summary>
        /// Record form submission and trigger downstream effects.
        /// </summary>
        public async Task OnFormSubmittedAsync(
            Guid tenantId, Guid patientPathwayId, Guid formId, Dictionary<string, object> responses)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                throw new InvalidOperationException("Pathway not found");

            // Store form responses in pathway data
            var pathwayData = string.IsNullOrEmpty(pathway.PathwayData)
                ? new { completedForms = new List<Guid> { formId }, formResponses = new Dictionary<string, object>() }
                : JsonSerializer.Deserialize<dynamic>(pathway.PathwayData);

            pathway.PathwayData = JsonSerializer.Serialize(new
            {
                completedForms = new List<Guid> { formId },
                formResponses = responses,
                lastFormSubmittedAt = DateTime.UtcNow
            });

            await _pathwayRepository.UpdateAsync(pathway);
            await _pathwayRepository.SaveChangesAsync();

            // Check if form submission triggers any actions
            var form = await _formRepository.GetByIdAsync(formId);
            if (form != null && !string.IsNullOrEmpty(form.OnSubmitActions))
            {
                await ExecuteFormSubmitActions(tenantId, patientPathwayId, form.OnSubmitActions);
            }

            // Re-evaluate rules
            await EvaluateBusinessRulesAsync(tenantId, patientPathwayId);
        }

        /// <summary>
        /// Get tasks for current stage.
        /// For now, this returns empty list. A future enhancement would link TaskItem to PatientPathway.
        /// </summary>
        public async Task<List<TaskItem>> GetStageTasksAsync(Guid tenantId, Guid patientPathwayId)
        {
            // TaskItem entity currently doesn't have PatientPathwayId
            // In Phase 4, extend TaskItem to support pathway-specific task tracking
            return await Task.FromResult(new List<TaskItem>());
        }

        /// <summary>
        /// Complete and discharge patient from program.
        /// </summary>
        public async Task<PatientPathway> CompletePathwayAsync(
            Guid tenantId, Guid patientPathwayId, string? notes = null)
        {
            var pathway = await _pathwayRepository.GetByIdAsync(patientPathwayId);
            if (pathway == null || pathway.TenantId != tenantId)
                throw new InvalidOperationException("Pathway not found");

            pathway.IsActive = false;
            pathway.CurrentStatus = "Completed";
            pathway.CompletedAt = DateTime.UtcNow;
            pathway.TransitionNotes = notes ?? "Program completed";

            await _pathwayRepository.UpdateAsync(pathway);
            await _pathwayRepository.SaveChangesAsync();

            return pathway;
        }

        // ==================== PRIVATE HELPERS ====================

        private async Task TriggerStageTasksAsync(Guid tenantId, Guid patientPathwayId, ProcessStage stage)
        {
            if (string.IsNullOrEmpty(stage.TriggeredTaskIds))
                return;

            // Phase 3.5: Task definition exists, automatic task creation would integrate here
            // In Phase 4, extend to actually create TaskItem instances with pathway linkage
            // For now, this is a placeholder for future task automation
            await Task.CompletedTask;
        }

        private bool ValidateStatusForStage(ProcessStage stage, string status)
        {
            if (string.IsNullOrEmpty(stage.AllowedStatuses))
                return true;

            var allowed = stage.AllowedStatuses.Split(',').Select(s => s.Trim());
            return allowed.Contains(status);
        }

        private bool EvaluateCondition(string conditionJson, PatientPathway pathway)
        {
            // Simplified condition evaluation
            // In production, use a dedicated rules engine (e.g., Rules Engine library)
            try
            {
                var condition = JsonDocument.Parse(conditionJson);
                // For now, always return true to trigger actions
                // Complex logic-driven parsing would go here
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool EvaluateVisibilityCondition(string conditionJson, PatientPathway pathway)
        {
            // Evaluate if form should be shown based on pathway data
            try
            {
                // Simplified logic
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteRuleAction(Guid tenantId, Guid patientPathwayId, string actionJson)
        {
            // Parse and execute action (transition, create task, send notification, etc.)
            try
            {
                var action = JsonDocument.Parse(actionJson);
                // Example: if action.Type == "Transition", call TransitionToStageAsync
                // This would be expanded based on action types defined
            }
            catch
            {
                // Log execution failure
            }
        }

        private async Task ExecuteFormSubmitActions(Guid tenantId, Guid patientPathwayId, string actionsJson)
        {
            try
            {
                var actions = JsonDocument.Parse(actionsJson);
                // Parse transitions, task creations, notifications, etc.
                // Execute each action based on configuration
            }
            catch
            {
                // Log execution failure
            }
        }

        /// <summary>
        /// Emits a workflow event to the timeline for a patient pathway.
        /// Creates audit/timeline record and triggers event-based automation.
        /// </summary>
        private async Task EmitWorkflowEventAsync(
            Guid tenantId,
            Guid patientPathwayId,
            string eventType,
            string description,
            string triggeredBy,
            object? eventData = null)
        {
            try
            {
                var workflowEvent = new WorkflowEvent
                {
                    TenantId = tenantId,
                    PatientPathwayId = patientPathwayId,
                    EventType = eventType,
                    Description = description,
                    TriggeredBy = triggeredBy,
                    EventData = eventData != null ? JsonSerializer.Serialize(eventData) : null,
                    VisibleToPatient = IsPatientVisibleEvent(eventType),
                    EventOccurredAt = DateTime.UtcNow
                };

                await _eventRepository.AddAsync(workflowEvent);
                await _eventRepository.SaveChangesAsync();

                // Trigger event-based automation (e.g., send notification, create task)
                await _triggerService.ProcessEventBasedTriggerAsync(tenantId, patientPathwayId, eventType);
            }
            catch (Exception ex)
            {
                // Log but don't throw - event emission should not break main workflow
                Console.WriteLine($"Error emitting workflow event: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if an event type should be visible to the patient
        /// (clinical events stay hidden; patient-relevant events are visible)
        /// </summary>
        private bool IsPatientVisibleEvent(string eventType)
        {
            var patientVisibleEvents = new[] 
            { 
                "Enrolled", 
                "StageTransitioned", 
                "QuestionnaireScheduled",
                "QuestionnaireSubmitted",
                "TaskAssigned",
                "AppointmentScheduled"
            };

            return patientVisibleEvents.Contains(eventType);
        }
    }
}
