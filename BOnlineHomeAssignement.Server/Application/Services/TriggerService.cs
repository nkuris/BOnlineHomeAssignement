using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Service for managing automated triggers in the workflow.
    /// Handles both time-based triggers (e.g., 7 days after enrollment) and 
    /// event-based triggers (e.g., on form submission).
    /// Includes retry logic, deduplication, and failure tracking.
    /// </summary>
    public class TriggerService
    {
        private readonly AppDbContext _dbContext;

        public TriggerService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Checks for and processes all pending time-based triggers.
        /// Called periodically (e.g., every 5 minutes) by background job.
        /// </summary>
        public async Task ProcessPendingTimeBasedTriggersAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;

            // Get all pending time-based triggers that are ready to execute
            var pendingTriggers = await _dbContext.TriggerExecutions
                .Where(te => te.TenantId == tenantId
                    && te.TriggerType.StartsWith("TimeBasedFollowUp")
                    && te.Status == "Pending"
                    && te.ScheduledAt <= now)
                .Include(te => te.PatientPathway)
                .ToListAsync();

            foreach (var trigger in pendingTriggers)
            {
                await ExecuteTriggerAsync(trigger);
            }
        }

        /// <summary>
        /// Processes a single trigger execution: attempts to create tasks/actions, 
        /// handles retries and deduplication.
        /// </summary>
        private async Task ExecuteTriggerAsync(TriggerExecution trigger)
        {
            // Check for duplicates (same trigger, same pathway, same day)
            var dedupKey = $"{trigger.TriggerType}_{trigger.PatientPathwayId}_{DateTime.UtcNow:yyyy-MM-dd}";
            var isDuplicate = await _dbContext.TriggerExecutions
                .AnyAsync(te => te.TenantId == trigger.TenantId
                    && te.DeduplicationKey == dedupKey
                    && te.Status == "Succeeded"
                    && te.Id != trigger.Id);

            if (isDuplicate)
            {
                trigger.Status = "Succeeded";
                trigger.ExecutionResult = "Duplicate_Skipped";
                trigger.CompletedAt = DateTime.UtcNow;
                _dbContext.TriggerExecutions.Update(trigger);
                await _dbContext.SaveChangesAsync();
                return;
            }

            try
            {
                trigger.Status = "Processing";
                trigger.StartedAt = DateTime.UtcNow;
                trigger.AttemptCount++;
                trigger.DeduplicationKey = dedupKey;
                _dbContext.TriggerExecutions.Update(trigger);
                await _dbContext.SaveChangesAsync();

                // Execute: Create tasks based on trigger configuration
                // Get task definitions for this program pathway
                var programConfig = trigger.PatientPathway?.ProgramConfigurationId;
                if (programConfig.HasValue)
                {
                    var taskDefs = await _dbContext.TaskDefinitions
                        .Where(td => td.TenantId == trigger.TenantId
                            && td.ProgramConfigurationId == programConfig.Value)
                        .ToListAsync();

                    foreach (var taskDef in taskDefs)
                    {
                        var newTask = new TaskItem
                        {
                            TenantId = trigger.TenantId,
                            PatientId = trigger.PatientPathway!.PatientId,
                            EnrollmentId = trigger.PatientPathway.EnrollmentId,
                            ProgramId = trigger.PatientPathway.ProgramConfigurationId,
                            Title = taskDef.Title ?? "Automated Task",
                            Description = taskDef.Description,
                            TaskType = taskDef.TaskType ?? "FollowUp",
                            Status = "Pending",
                            Priority = "Medium",
                            CreationSource = "Automatic",
                            CreatedBySourceId = trigger.Id.ToString(),
                            DueDate = DateTime.UtcNow.AddDays(taskDef.DueDays ?? 1)
                        };

                        _dbContext.TaskItems.Add(newTask);
                    }
                }

                trigger.Status = "Succeeded";
                trigger.ExecutionResult = "TasksCreated";
                trigger.CompletedAt = DateTime.UtcNow;
                _dbContext.TriggerExecutions.Update(trigger);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                trigger.LastError = ex.Message;
                trigger.AttemptCount++;

                if (trigger.AttemptCount >= trigger.MaxRetries)
                {
                    trigger.Status = "Failed";
                    trigger.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    trigger.Status = "Pending";
                    trigger.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, trigger.AttemptCount)); // Exponential backoff
                }

                _dbContext.TriggerExecutions.Update(trigger);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Processes event-based triggers (e.g., form submitted, stage transitioned).
        /// Called immediately when an event occurs.
        /// </summary>
        public async Task ProcessEventBasedTriggerAsync(Guid tenantId, Guid patientPathwayId, string eventType)
        {
            try
            {
                var pathway = await _dbContext.PatientPathways.FindAsync(patientPathwayId);
                if (pathway == null) return;

                // Find business rules that trigger on this event
                var triggers = await _dbContext.BusinessRules
                    .Where(br => br.TenantId == tenantId
                        && br.TriggerEvent == eventType
                        && br.IsActive)
                    .Include(br => br.ProgramConfiguration)
                    .ToListAsync();

                foreach (var rule in triggers)
                {
                    // Create trigger execution record
                    var execution = new TriggerExecution
                    {
                        TenantId = tenantId,
                        PatientPathwayId = patientPathwayId,
                        TriggerType = $"EventBasedTrigger_{eventType}",
                        TriggerConfigId = rule.Id.ToString(),
                        Status = "Pending",
                        ScheduledAt = DateTime.UtcNow,
                        MaxRetries = 1 // Event-based triggers don't retry much
                    };

                    _dbContext.TriggerExecutions.Add(execution);

                    // Execute immediately
                    await ExecuteEventTriggerAsync(execution, rule);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - event triggers should not break the main flow
                Console.WriteLine($"Event trigger error: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes an event-based trigger by evaluating the business rule.
        /// </summary>
        private async Task ExecuteEventTriggerAsync(TriggerExecution execution, BusinessRule rule)
        {
            try
            {
                execution.Status = "Processing";
                execution.StartedAt = DateTime.UtcNow;
                _dbContext.TriggerExecutions.Update(execution);

                // Evaluate rule condition (simplified - in production, evaluate JSON expression)
                bool conditionMet = EvaluateCondition(rule.Condition, execution.PatientPathwayId);

                if (conditionMet)
                {
                    // Execute rule action (simplified - parse JSON action)
                    // Actions: CreateTask, TransitionStage, SendMessage, EscalateToNurse
                    var actionType = rule.Action?.Split(':')[0] ?? "Unknown";

                    switch (actionType)
                    {
                        case "CreateTask":
                            await CreateTaskFromActionAsync(execution, rule.Action);
                            break;
                        case "TransitionStage":
                            // Future: auto-advance stage
                            break;
                        case "SendMessage":
                            // Future: queue communication message
                            break;
                    }

                    execution.ExecutionResult = $"Action_{actionType}_Applied";
                }
                else
                {
                    execution.ExecutionResult = "ConditionNotMet";
                }

                execution.Status = "Succeeded";
                execution.CompletedAt = DateTime.UtcNow;
                _dbContext.TriggerExecutions.Update(execution);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                execution.Status = "Failed";
                execution.LastError = ex.Message;
                execution.CompletedAt = DateTime.UtcNow;
                _dbContext.TriggerExecutions.Update(execution);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Simplified condition evaluation - in production, parse JSON condition
        /// </summary>
        private bool EvaluateCondition(string condition, Guid pathwayId)
        {
            // Placeholder: In production, parse JSON condition and evaluate
            // Example: condition = "{ patientType: 'Chronic', stage: 'Initial' }"
            return true;
        }

        /// <summary>
        /// Parses action string and creates appropriate task
        /// </summary>
        private async Task CreateTaskFromActionAsync(TriggerExecution execution, string action)
        {
            // Parse action: e.g., "CreateTask:FollowUpCall:Medium"
            var parts = action?.Split(':') ?? new[] { "CreateTask" };
            var taskType = parts.Length > 1 ? parts[1] : "FollowUp";
            var priority = parts.Length > 2 ? parts[2] : "Medium";

            var pathway = await _dbContext.PatientPathways.FindAsync(execution.PatientPathwayId);
            if (pathway == null) return;

            var task = new TaskItem
            {
                TenantId = execution.TenantId,
                PatientId = pathway.PatientId,
                EnrollmentId = pathway.EnrollmentId,
                ProgramId = pathway.ProgramConfigurationId,
                Title = $"Automated {taskType} Task",
                TaskType = taskType,
                Status = "Pending",
                Priority = priority,
                CreationSource = "Automatic",
                CreatedBySourceId = execution.Id.ToString(),
                DueDate = DateTime.UtcNow.AddDays(3)
            };

            _dbContext.TaskItems.Add(task);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Processes failed triggers that can be retried
        /// </summary>
        public async Task ProcessFailedTriggersForRetryAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;

            var failedTriggers = await _dbContext.TriggerExecutions
                .Where(te => te.TenantId == tenantId
                    && te.Status == "Pending"
                    && te.NextRetryAt <= now
                    && te.AttemptCount < te.MaxRetries)
                .ToListAsync();

            foreach (var trigger in failedTriggers)
            {
                await ExecuteTriggerAsync(trigger);
            }
        }

        /// <summary>
        /// Schedules a time-based trigger (e.g., 7 days after enrollment)
        /// </summary>
        public async Task ScheduleFollowUpTriggerAsync(
            Guid tenantId,
            Guid patientPathwayId,
            string triggerType,
            int delayInDays)
        {
            var execution = new TriggerExecution
            {
                TenantId = tenantId,
                PatientPathwayId = patientPathwayId,
                TriggerType = triggerType,
                Status = "Pending",
                ScheduledAt = DateTime.UtcNow.AddDays(delayInDays),
                MaxRetries = 3,
                AttemptCount = 0
            };

            _dbContext.TriggerExecutions.Add(execution);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all trigger executions for a patient pathway (for timeline/audit)
        /// </summary>
        public async Task<List<TriggerExecution>> GetPathwayTriggerHistoryAsync(Guid tenantId, Guid patientPathwayId)
        {
            return await _dbContext.TriggerExecutions
                .Where(te => te.TenantId == tenantId && te.PatientPathwayId == patientPathwayId)
                .OrderByDescending(te => te.ScheduledAt)
                .ToListAsync();
        }
    }
}