using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Service for managing questionnaires/surveys in patient workflow.
    /// Handles scheduling, recurrence patterns, responses, and integration with triggers.
    /// </summary>
    public class QuestionnairesService
    {
        private readonly AppDbContext _dbContext;
        private readonly CommunicationService _communicationService;

        public QuestionnairesService(
            AppDbContext dbContext,
            CommunicationService communicationService)
        {
            _dbContext = dbContext;
            _communicationService = communicationService;
        }

        /// <summary>
        /// Creates a new questionnaire for a program
        /// </summary>
        public async Task<Questionnaire> CreateQuestionnaireAsync(
            Guid tenantId,
            Guid programConfigurationId,
            string title,
            string? description,
            string? fieldsDefinition,
            string recurrenceType = "Once",
            string? recurrenceValue = null)
        {
            var questionnaire = new Questionnaire
            {
                TenantId = tenantId,
                ProgramConfigurationId = programConfigurationId,
                Title = title,
                Description = description,
                FieldsDefinition = fieldsDefinition,
                RecurrenceType = recurrenceType,
                RecurrenceValue = recurrenceValue,
                IsActive = true
            };

            _dbContext.Questionnaires.Add(questionnaire);
            await _dbContext.SaveChangesAsync();
            return questionnaire;
        }

        /// <summary>
        /// Schedules questionnaire sending for all active pathways in a program at a specific recurrence
        /// </summary>
        public async Task ProcessQuestionnaireScheduleAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;

            // Get all active questionnaires
            var questionnaires = await _dbContext.Questionnaires
                .Where(q => q.TenantId == tenantId && q.IsActive)
                .ToListAsync();

            foreach (var questionnaire in questionnaires)
            {
                var pathwaysToSend = await DeterminePathwaysForQuestionnaireAsync(questionnaire, now);

                foreach (var pathway in pathwaysToSend)
                {
                    // Check if already sent today (dedup)
                    var alreadySent = await _dbContext.QuestionnaireResponses
                        .AnyAsync(qr => qr.TenantId == tenantId
                            && qr.QuestionnaireId == questionnaire.Id
                            && qr.PatientPathwayId == pathway.Id
                            && qr.SentAt.Date == now.Date);

                    if (alreadySent) continue;

                    // Create response record (pending)
                    var response = new QuestionnaireResponse
                    {
                        TenantId = tenantId,
                        QuestionnaireId = questionnaire.Id,
                        PatientPathwayId = pathway.Id,
                        ResponseData = "{}",
                        SentAt = now,
                        Status = "Pending"
                    };

                    _dbContext.QuestionnaireResponses.Add(response);

                    // Queue communication message (Email, SMS, etc.)
                    await QueueQuestionnaireMessageAsync(questionnaire, pathway, response);
                }
            }
        }

        /// <summary>
        /// Determines which pathways should receive this questionnaire based on recurrence pattern
        /// </summary>
        private async Task<List<PatientPathway>> DeterminePathwaysForQuestionnaireAsync(
            Questionnaire questionnaire,
            DateTime now)
        {
            var pathways = await _dbContext.PatientPathways
                .Where(pp => pp.TenantId == questionnaire.TenantId
                    && pp.ProgramConfigurationId == questionnaire.ProgramConfigurationId)
                .ToListAsync();

            return questionnaire.RecurrenceType switch
            {
                "Once" => pathways.Where(p => !HasQuestionnaireBeenSent(p, questionnaire)).ToList(),
                "Weekly" => pathways.Where(p => IsWeeklyRecurrenceDay(now, questionnaire.RecurrenceValue)).ToList(),
                "Monthly" => pathways.Where(p => IsMonthlyRecurrenceDay(now, questionnaire.RecurrenceValue)).ToList(),
                "DaysAfterEnrollment" => pathways
                    .Where(p => ShouldSendDaysAfterEnrollment(p, questionnaire.RecurrenceValue, now))
                    .ToList(),
                "OnEvent" => pathways, // Events handled separately
                _ => new List<PatientPathway>()
            };
        }

        /// <summary>
        /// Checks if questionnaire was already sent to this pathway
        /// </summary>
        private bool HasQuestionnaireBeenSent(PatientPathway pathway, Questionnaire questionnaire)
        {
            return _dbContext.QuestionnaireResponses
                .Any(qr => qr.PatientPathwayId == pathway.Id && qr.QuestionnaireId == questionnaire.Id);
        }

        /// <summary>
        /// Checks if today is the weekly recurrence day
        /// </summary>
        private bool IsWeeklyRecurrenceDay(DateTime now, string? dayOfWeek)
        {
            if (string.IsNullOrEmpty(dayOfWeek)) return true;
            return now.DayOfWeek.ToString() == dayOfWeek;
        }

        /// <summary>
        /// Checks if today is the monthly recurrence day
        /// </summary>
        private bool IsMonthlyRecurrenceDay(DateTime now, string? dayOfMonth)
        {
            if (string.IsNullOrEmpty(dayOfMonth)) return now.Day == 1;
            return int.TryParse(dayOfMonth, out var day) && now.Day == day;
        }

        /// <summary>
        /// Checks if enough days have passed since enrollment
        /// </summary>
        private bool ShouldSendDaysAfterEnrollment(PatientPathway pathway, string? daysStr, DateTime now)
        {
            if (!int.TryParse(daysStr, out var days)) return false;

            var daysSinceEnrollment = (now - pathway.CreatedAt).TotalDays;
            return daysSinceEnrollment >= days && daysSinceEnrollment < days + 1; // Within 1-day window
        }

        /// <summary>
        /// Queues a communication message for questionnaire sending
        /// </summary>
        private async Task QueueQuestionnaireMessageAsync(
            Questionnaire questionnaire,
            PatientPathway pathway,
            QuestionnaireResponse response)
        {
            var patient = await _dbContext.Patients.FindAsync(pathway.PatientId);
            if (patient == null) return;

            var subject = $"Survey: {questionnaire.Title}";
            var messageBody = $@"
Hello {patient.FirstName},

We'd like to hear from you. Please take a moment to complete this questionnaire:

{questionnaire.Description ?? ""}

Response ID: {response.Id}

Thank you!
";

            // Queue via communication service (Email by default)
            await _communicationService.QueueMessageAsync(
                tenantId: questionnaire.TenantId,
                patientPathwayId: pathway.Id,
                channel: "Email",
                recipient: patient.Email ?? "",
                subject: subject,
                messageBody: messageBody,
                messageType: "Questionnaire",
                relatedEntityId: questionnaire.Id.ToString(),
                relatedEntityType: "Questionnaire");
        }

        /// <summary>
        /// Submits a questionnaire response
        /// </summary>
        public async Task<QuestionnaireResponse> SubmitQuestionnaireResponseAsync(
            Guid tenantId,
            Guid responseId,
            Dictionary<string, object> answers)
        {
            var response = await _dbContext.QuestionnaireResponses.FindAsync(responseId);
            if (response == null)
                throw new InvalidOperationException("Questionnaire response not found");

            if (response.TenantId != tenantId)
                throw new UnauthorizedAccessException("Access denied");

            response.ResponseData = JsonSerializer.Serialize(answers);
            response.CompletedAt = DateTime.UtcNow;
            response.Status = "Completed";

            _dbContext.QuestionnaireResponses.Update(response);

            // Optionally trigger event-based actions from questionnaire response
            await _dbContext.SaveChangesAsync();

            return response;
        }

        /// <summary>
        /// Gets pending questionnaires for a patient pathway
        /// </summary>
        public async Task<List<QuestionnaireResponse>> GetPendingQuestionnairesAsync(
            Guid tenantId,
            Guid patientPathwayId)
        {
            return await _dbContext.QuestionnaireResponses
                .Where(qr => qr.TenantId == tenantId
                    && qr.PatientPathwayId == patientPathwayId
                    && qr.Status == "Pending")
                .Include(qr => qr.Questionnaire)
                .OrderByDescending(qr => qr.SentAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets completed questionnaires for a patient pathway
        /// </summary>
        public async Task<List<QuestionnaireResponse>> GetCompletedQuestionnairesAsync(
            Guid tenantId,
            Guid patientPathwayId)
        {
            return await _dbContext.QuestionnaireResponses
                .Where(qr => qr.TenantId == tenantId
                    && qr.PatientPathwayId == patientPathwayId
                    && qr.Status == "Completed")
                .Include(qr => qr.Questionnaire)
                .OrderByDescending(qr => qr.CompletedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all questionnaire responses for a questionnaire (admin view)
        /// </summary>
        public async Task<List<QuestionnaireResponse>> GetQuestionnaireResponsesAsync(
            Guid tenantId,
            Guid questionnaireId)
        {
            return await _dbContext.QuestionnaireResponses
                .Where(qr => qr.TenantId == tenantId && qr.QuestionnaireId == questionnaireId)
                .Include(qr => qr.PatientPathway)
                .OrderByDescending(qr => qr.CompletedAt ?? qr.SentAt)
                .ToListAsync();
        }

        /// <summary>
        /// Deactivates a questionnaire (soft delete)
        /// </summary>
        public async Task DeactivateQuestionnaireAsync(Guid tenantId, Guid questionnaireId)
        {
            var questionnaire = await _dbContext.Questionnaires.FindAsync(questionnaireId);
            if (questionnaire == null)
                throw new InvalidOperationException("Questionnaire not found");

            if (questionnaire.TenantId != tenantId)
                throw new UnauthorizedAccessException("Access denied");

            questionnaire.IsActive = false;
            _dbContext.Questionnaires.Update(questionnaire);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all questionnaires for a program
        /// </summary>
        public async Task<List<Questionnaire>> GetProgramQuestionnairesAsync(
            Guid tenantId,
            Guid programConfigurationId)
        {
            return await _dbContext.Questionnaires
                .Where(q => q.TenantId == tenantId && q.ProgramConfigurationId == programConfigurationId)
                .OrderBy(q => q.Title)
                .ToListAsync();
        }
    }
}