using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Application.DTOs.Conversion;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using BOnlineHomeAssignement.Server.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using DomainProgram = BOnlineHomeAssignement.Server.Domain.Entities.Program;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    public interface ILeadConversionService
    {
        Task<ConversionResultDto> ConvertLeadToPatientAsync(Guid tenantId, Guid leadId);
        Task<BulkConversionResultDto> ConvertLeadsToPatientsBulkAsync(Guid tenantId, List<Guid> leadIds, bool ignoreErrors = false);
        Task<ConversionReadinessDto> CheckConversionReadinessAsync(Guid tenantId, Guid leadId);
        Task<ConversionHistoryDto?> GetConversionHistoryAsync(Guid tenantId, Guid leadId);
    }

    public class LeadConversionService : ILeadConversionService
    {
        private readonly IRepository<Lead> _leadRepository;
        private readonly IRepository<Patient> _patientRepository;
        private readonly IRepository<Enrollment> _enrollmentRepository;
        private readonly IRepository<DomainProgram> _programRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly ILogger<LeadConversionService> _logger;

        // Default program ID for auto-enrollment (seeded in SeedData)
        private static readonly Guid DefaultProgramId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        public LeadConversionService(
            IRepository<Lead> leadRepository,
            IRepository<Patient> patientRepository,
            IRepository<Enrollment> enrollmentRepository,
            IRepository<DomainProgram> programRepository,
            IRepository<AuditLog> auditLogRepository,
            ILogger<LeadConversionService> logger)
        {
            _leadRepository = leadRepository;
            _patientRepository = patientRepository;
            _enrollmentRepository = enrollmentRepository;
            _programRepository = programRepository;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<ConversionResultDto> ConvertLeadToPatientAsync(Guid tenantId, Guid leadId)
        {
            try
            {
                var lead = await _leadRepository.FirstOrDefaultAsync(
                    l => l.Id == leadId && l.TenantId == tenantId);

                if (lead == null)
                    return ConversionResultDto.Failure($"Lead {leadId} not found in tenant {tenantId}.");

                var (isValid, validationErrors) = ConversionHelper.ValidateLeadForConversion(lead);
                if (!isValid)
                    return ConversionResultDto.Failure("Lead validation failed: " + string.Join(", ", validationErrors));

                var duplicateCheck = await CheckForDuplicatePatientAsync(lead);
                if (duplicateCheck.HasDuplicate)
                {
                    return ConversionResultDto.Failure(
                        $"Duplicate patient found: {duplicateCheck.DuplicatePatientId} " +
                        $"(Email/Phone match, confidence: {duplicateCheck.Confidence}%)");
                }

                var enrichedFields = ConversionHelper.ExtractPatientFields(lead, lead.LeadSource);
                var patient = ConversionHelper.CreatePatientFromLead(lead, enrichedFields);

                await _patientRepository.AddAsync(patient);
                await _patientRepository.SaveChangesAsync();

                // Auto-enroll the patient in the default program
                Guid? enrollmentId = null;
                string? programName = null;
                try
                {
                    var program = await _programRepository.FirstOrDefaultAsync(p => p.Id == DefaultProgramId);
                    if (program != null)
                    {
                        var enrollment = new Enrollment
                        {
                            Id = Guid.NewGuid(),
                            PatientId = patient.Id,
                            ProgramId = program.Id,
                            EnrolledAt = DateTime.UtcNow,
                            Status = "Active"
                        };

                        await _enrollmentRepository.AddAsync(enrollment);
                        await _enrollmentRepository.SaveChangesAsync();

                        enrollmentId = enrollment.Id;
                        programName = program.Name;

                        _logger.LogInformation($"Patient {patient.Id} auto-enrolled in program {program.Name}");
                    }
                    else
                    {
                        _logger.LogWarning($"Default program {DefaultProgramId} not found. Patient enrolled without program link.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to create enrollment for patient {patient.Id}. Continuing without enrollment.");
                }

                await LogConversionAsync(tenantId, leadId, $"Lead '{lead.Name}' converted to Patient '{patient.FirstName} {patient.LastName}'");

                _logger.LogInformation($"Lead {leadId} converted to Patient {patient.Id}");
                return ConversionResultDto.Success(patient.Id, enrollmentId, programName ?? "", "Lead successfully converted to patient.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting lead {leadId}");
                return ConversionResultDto.Failure($"Conversion failed: {ex.Message}");
            }
        }

        public async Task<BulkConversionResultDto> ConvertLeadsToPatientsBulkAsync(
            Guid tenantId,
            List<Guid> leadIds,
            bool ignoreErrors = false)
        {
            if (leadIds == null || leadIds.Count == 0)
                return new BulkConversionResultDto
                {
                    TotalRequested = 0,
                    SuccessfulConversions = new List<Guid>(),
                    FailedLeads = new Dictionary<Guid, string>()
                };

            var successfulPatientIds = new List<Guid>();
            var failedLeads = new Dictionary<Guid, string>();

            foreach (var leadId in leadIds)
            {
                var result = await ConvertLeadToPatientAsync(tenantId, leadId);
                if (result.IsSuccess)
                {
                    successfulPatientIds.Add(result.PatientId!.Value);
                }
                else
                {
                    failedLeads[leadId] = result.ErrorMessage ?? "Unknown error";
                    if (!ignoreErrors)
                        break;
                }
            }

            return new BulkConversionResultDto
            {
                TotalRequested = leadIds.Count,
                SuccessfulConversions = successfulPatientIds,
                FailedLeads = failedLeads
            };
        }

        public async Task<ConversionReadinessDto> CheckConversionReadinessAsync(Guid tenantId, Guid leadId)
        {
            try
            {
                var lead = await _leadRepository.FirstOrDefaultAsync(
                    l => l.Id == leadId && l.TenantId == tenantId);

                if (lead == null)
                    return new ConversionReadinessDto
                    {
                        IsReady = false,
                        Reason = "Lead not found",
                        Issues = new List<string> { "Lead does not exist" }
                    };

                var issues = new List<string>();
                var (isValid, validationErrors) = ConversionHelper.ValidateLeadForConversion(lead);

                if (!isValid)
                {
                    issues.AddRange(validationErrors);
                    return new ConversionReadinessDto
                    {
                        IsReady = false,
                        Reason = "Lead validation failed",
                        Issues = issues
                    };
                }

                var duplicateCheck = await CheckForDuplicatePatientAsync(lead);
                if (duplicateCheck.HasDuplicate)
                {
                    issues.Add($"Duplicate patient likely: {duplicateCheck.DuplicatePatientId} (Confidence: {duplicateCheck.Confidence}%)");
                    return new ConversionReadinessDto
                    {
                        IsReady = false,
                        Reason = "Duplicate patient detected",
                        Issues = issues,
                        PotentialDuplicatePatientId = duplicateCheck.DuplicatePatientId,
                        DuplicateConfidence = duplicateCheck.Confidence
                    };
                }

                return new ConversionReadinessDto
                {
                    IsReady = true,
                    Reason = "Lead is ready for conversion",
                    Issues = issues
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking conversion readiness for lead {leadId}");
                return new ConversionReadinessDto
                {
                    IsReady = false,
                    Reason = "Error checking readiness",
                    Issues = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ConversionHistoryDto?> GetConversionHistoryAsync(Guid tenantId, Guid leadId)
        {
            try
            {
                var lead = await _leadRepository.FirstOrDefaultAsync(
                    l => l.Id == leadId && l.TenantId == tenantId);

                if (lead == null)
                    return null;

                var auditLogs = await _auditLogRepository.FindAsync(
                    a => a.EntityId == leadId && a.EntityName == "Lead" && a.Action.Contains("Converted"));

                var conversionLog = auditLogs.FirstOrDefault();
                if (conversionLog == null)
                    return null;

                return new ConversionHistoryDto
                {
                    LeadId = leadId,
                    PatientId = Guid.Empty,
                    ConvertedAt = conversionLog.ChangedAt,
                    ConversionDetails = conversionLog.Changes ?? "No details recorded"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving conversion history for lead {leadId}");
                return null;
            }
        }

        private async Task<(bool HasDuplicate, Guid? DuplicatePatientId, int Confidence)> CheckForDuplicatePatientAsync(Lead lead)
        {
            var existingPatients = await _patientRepository.FindAsync(
                p => p.TenantId == lead.TenantId);

            foreach (var patient in existingPatients)
            {
                var likelihood = ConversionHelper.GetPatientDuplicateLikelihood(lead, patient);
                if (likelihood >= 75)
                    return (true, patient.Id, likelihood);
            }

            return (false, null, 0);
        }

        private async Task LogConversionAsync(Guid tenantId, Guid leadId, string description)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Lead",
                EntityId = leadId,
                Action = "Converted to Patient",
                Changes = description,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = null
            };

            await _auditLogRepository.AddAsync(auditLog);
            await _auditLogRepository.SaveChangesAsync();
        }
    }
}
