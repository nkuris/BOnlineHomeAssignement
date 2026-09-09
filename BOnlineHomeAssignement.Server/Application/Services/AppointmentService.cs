using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Application.DTOs.Appointment;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Service for managing appointments.
    /// Provides CRUD operations and scheduling logic (foundational; advanced scheduling features in Phase 3).
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>Get all appointments for a patient.</summary>
        Task<IEnumerable<AppointmentResponseDto>> GetPatientAppointmentsAsync(Guid tenantId, Guid patientId);

        /// <summary>Get all appointments for a tenant (pagination support for large volumes).</summary>
        Task<IEnumerable<AppointmentResponseDto>> GetTenantAppointmentsAsync(Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>Get a specific appointment by ID.</summary>
        Task<AppointmentResponseDto?> GetAppointmentAsync(Guid tenantId, Guid appointmentId);

        /// <summary>Create a new appointment for a patient.</summary>
        Task<AppointmentResponseDto> CreateAppointmentAsync(Guid tenantId, Guid patientId, CreateAppointmentDto dto);

        /// <summary>Create appointment(s) from lead conversion (e.g., auto-schedule initial consultation).</summary>
        Task<AppointmentResponseDto> CreateAppointmentFromLeadConversionAsync(
            Guid tenantId,
            Guid patientId,
            Guid sourceLeadId,
            CreateAppointmentDto dto);

        /// <summary>Update an existing appointment.</summary>
        Task<AppointmentResponseDto> UpdateAppointmentAsync(Guid tenantId, Guid appointmentId, UpdateAppointmentDto dto);

        /// <summary>Delete an appointment (soft delete via status change).</summary>
        Task DeleteAppointmentAsync(Guid tenantId, Guid appointmentId);

        /// <summary>Update appointment status (Scheduled, Confirmed, Completed, Cancelled, No-Show).</summary>
        Task<AppointmentResponseDto> UpdateAppointmentStatusAsync(Guid tenantId, Guid appointmentId, string newStatus);

        /// <summary>Get available time slots for a given date and provider (placeholder for Phase 3).</summary>
        Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(Guid tenantId, string providerId, DateTime date);
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Patient> _patientRepository;
        private readonly IRepository<Enrollment> _enrollmentRepository;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IRepository<Appointment> appointmentRepository,
            IRepository<Patient> patientRepository,
            IRepository<Enrollment> enrollmentRepository,
            ILogger<AppointmentService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _enrollmentRepository = enrollmentRepository;
            _logger = logger;
        }

        /// <summary>Get all appointments for a patient.</summary>
        public async Task<IEnumerable<AppointmentResponseDto>> GetPatientAppointmentsAsync(Guid tenantId, Guid patientId)
        {
            var appointments = await _appointmentRepository.FindAsync(
                a => a.TenantId == tenantId && a.PatientId == patientId);

            return appointments
                .OrderByDescending(a => a.ScheduledStart)
                .Select(MapToResponseDto);
        }

        /// <summary>Get all appointments for a tenant (with optional date range filtering).</summary>
        public async Task<IEnumerable<AppointmentResponseDto>> GetTenantAppointmentsAsync(
            Guid tenantId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var appointments = await _appointmentRepository.FindAsync(
                a => a.TenantId == tenantId &&
                     (fromDate == null || a.ScheduledStart >= fromDate) &&
                     (toDate == null || a.ScheduledStart <= toDate));

            return appointments
                .OrderBy(a => a.ScheduledStart)
                .Select(MapToResponseDto);
        }

        /// <summary>Get a specific appointment by ID.</summary>
        public async Task<AppointmentResponseDto?> GetAppointmentAsync(Guid tenantId, Guid appointmentId)
        {
            var appointment = await _appointmentRepository.FirstOrDefaultAsync(
                a => a.Id == appointmentId && a.TenantId == tenantId);

            return appointment != null ? MapToResponseDto(appointment) : null;
        }

        /// <summary>Create a new appointment for a patient.</summary>
        public async Task<AppointmentResponseDto> CreateAppointmentAsync(Guid tenantId, Guid patientId, CreateAppointmentDto dto)
        {
            try
            {
                // Verify patient exists
                var patient = await _patientRepository.FirstOrDefaultAsync(
                    p => p.Id == patientId && p.TenantId == tenantId);

                if (patient == null)
                    throw new InvalidOperationException($"Patient {patientId} not found in tenant {tenantId}");

                // If EnrollmentId is provided, verify enrollment exists and belongs to this patient
                Guid? enrollmentId = dto.EnrollmentId;
                if (enrollmentId.HasValue && enrollmentId.Value != Guid.Empty)
                {
                    var enrollment = await _enrollmentRepository.FirstOrDefaultAsync(
                        e => e.Id == enrollmentId && e.PatientId == patientId);

                    if (enrollment == null)
                        throw new InvalidOperationException($"Enrollment {enrollmentId} not found for patient {patientId}");
                }

                // Validate appointment times
                if (dto.ScheduledStart < DateTime.UtcNow)
                    throw new InvalidOperationException("Appointment cannot be scheduled in the past");

                if (dto.ScheduledEnd.HasValue && dto.ScheduledEnd <= dto.ScheduledStart)
                    throw new InvalidOperationException("Scheduled end time must be after start time");

                // Create appointment
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PatientId = patientId,
                    EnrollmentId = enrollmentId,
                    Title = dto.Title,
                    Description = dto.Description,
                    ScheduledStart = dto.ScheduledStart,
                    ScheduledEnd = dto.ScheduledEnd,
                    DurationMinutes = dto.DurationMinutes,
                    Status = "Scheduled",
                    AppointmentType = dto.AppointmentType ?? "In-Person",
                    Location = dto.Location,
                    ProviderId = dto.ProviderId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _appointmentRepository.AddAsync(appointment);
                await _appointmentRepository.SaveChangesAsync();

                _logger.LogInformation(
                    $"Appointment {appointment.Id} created for patient {patientId} on {appointment.ScheduledStart}");

                return MapToResponseDto(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating appointment for patient {patientId}");
                throw;
            }
        }

        /// <summary>Create appointment from lead conversion (useful for auto-scheduling initial consultations).</summary>
        public async Task<AppointmentResponseDto> CreateAppointmentFromLeadConversionAsync(
            Guid tenantId,
            Guid patientId,
            Guid sourceLeadId,
            CreateAppointmentDto dto)
        {
            try
            {
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PatientId = patientId,
                    Title = dto.Title,
                    Description = dto.Description,
                    ScheduledStart = dto.ScheduledStart,
                    ScheduledEnd = dto.ScheduledEnd,
                    DurationMinutes = dto.DurationMinutes,
                    Status = "Scheduled",
                    AppointmentType = dto.AppointmentType ?? "In-Person",
                    Location = dto.Location,
                    ProviderId = dto.ProviderId,
                    SourceLeadId = sourceLeadId, // Track that this appointment came from a lead conversion
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _appointmentRepository.AddAsync(appointment);
                await _appointmentRepository.SaveChangesAsync();

                _logger.LogInformation(
                    $"Appointment {appointment.Id} created from lead conversion {sourceLeadId}");

                return MapToResponseDto(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating appointment from lead {sourceLeadId}");
                throw;
            }
        }

        /// <summary>Update an appointment.</summary>
        public async Task<AppointmentResponseDto> UpdateAppointmentAsync(Guid tenantId, Guid appointmentId, UpdateAppointmentDto dto)
        {
            try
            {
                var appointment = await _appointmentRepository.FirstOrDefaultAsync(
                    a => a.Id == appointmentId && a.TenantId == tenantId);

                if (appointment == null)
                    throw new InvalidOperationException($"Appointment {appointmentId} not found");

                // Update fields
                if (!string.IsNullOrWhiteSpace(dto.Title))
                    appointment.Title = dto.Title;

                if (dto.Description != null)
                    appointment.Description = dto.Description;

                if (dto.ScheduledStart.HasValue)
                    appointment.ScheduledStart = dto.ScheduledStart.Value;

                if (dto.ScheduledEnd.HasValue)
                    appointment.ScheduledEnd = dto.ScheduledEnd.Value;

                if (dto.DurationMinutes.HasValue)
                    appointment.DurationMinutes = dto.DurationMinutes;

                if (!string.IsNullOrWhiteSpace(dto.AppointmentType))
                    appointment.AppointmentType = dto.AppointmentType;

                if (dto.Location != null)
                    appointment.Location = dto.Location;

                if (dto.ProviderId != null)
                    appointment.ProviderId = dto.ProviderId;

                if (dto.Notes != null)
                    appointment.Notes = dto.Notes;

                appointment.UpdatedAt = DateTime.UtcNow;

                await _appointmentRepository.UpdateAsync(appointment);
                await _appointmentRepository.SaveChangesAsync();

                _logger.LogInformation($"Appointment {appointmentId} updated");

                return MapToResponseDto(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating appointment {appointmentId}");
                throw;
            }
        }

        /// <summary>Delete an appointment (soft delete via status change).</summary>
        public async Task DeleteAppointmentAsync(Guid tenantId, Guid appointmentId)
        {
            try
            {
                var appointment = await _appointmentRepository.FirstOrDefaultAsync(
                    a => a.Id == appointmentId && a.TenantId == tenantId);

                if (appointment == null)
                    throw new InvalidOperationException($"Appointment {appointmentId} not found");

                // Soft delete by changing status
                appointment.Status = "Cancelled";
                appointment.UpdatedAt = DateTime.UtcNow;

                await _appointmentRepository.UpdateAsync(appointment);
                await _appointmentRepository.SaveChangesAsync();

                _logger.LogInformation($"Appointment {appointmentId} cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting appointment {appointmentId}");
                throw;
            }
        }

        /// <summary>Update appointment status.</summary>
        public async Task<AppointmentResponseDto> UpdateAppointmentStatusAsync(Guid tenantId, Guid appointmentId, string newStatus)
        {
            var validStatuses = new[] { "Scheduled", "Confirmed", "In Progress", "Completed", "Cancelled", "No-Show" };

            if (!validStatuses.Contains(newStatus, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid status '{newStatus}'. Valid statuses: {string.Join(", ", validStatuses)}");

            var appointment = await _appointmentRepository.FirstOrDefaultAsync(
                a => a.Id == appointmentId && a.TenantId == tenantId);

            if (appointment == null)
                throw new InvalidOperationException($"Appointment {appointmentId} not found");

            appointment.Status = newStatus;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _appointmentRepository.UpdateAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();

            _logger.LogInformation($"Appointment {appointmentId} status changed to {newStatus}");

            return MapToResponseDto(appointment);
        }

        /// <summary>Get available time slots for a provider on a specific date (placeholder for Phase 3).</summary>
        public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(Guid tenantId, string providerId, DateTime date)
        {
            // TODO: Phase 3 - Implement calendar/provider availability logic
            // For now, return placeholder response

            _logger.LogInformation($"Fetching available slots for provider {providerId} on {date.Date}");

            // This will be implemented in Phase 3 with:
            // - Provider availability calendars
            // - Booked appointment conflicts
            // - Break times and lunch hours
            // - Provider-specific duration rules
            // - Tenant-specific scheduling preferences

            return await Task.FromResult(new List<AvailableSlotDto>
            {
                new AvailableSlotDto { StartTime = new DateTime(date.Year, date.Month, date.Day, 09, 00, 0), EndTime = new DateTime(date.Year, date.Month, date.Day, 09, 30, 0) },
                new AvailableSlotDto { StartTime = new DateTime(date.Year, date.Month, date.Day, 10, 00, 0), EndTime = new DateTime(date.Year, date.Month, date.Day, 10, 30, 0) },
                new AvailableSlotDto { StartTime = new DateTime(date.Year, date.Month, date.Day, 14, 00, 0), EndTime = new DateTime(date.Year, date.Month, date.Day, 14, 30, 0) }
            });
        }

        /// <summary>Map Appointment to response DTO.</summary>
        private static AppointmentResponseDto MapToResponseDto(Appointment appointment)
        {
            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                TenantId = appointment.TenantId,
                PatientId = appointment.PatientId,
                EnrollmentId = appointment.EnrollmentId,
                Title = appointment.Title,
                Description = appointment.Description,
                ScheduledStart = appointment.ScheduledStart,
                ScheduledEnd = appointment.ScheduledEnd,
                DurationMinutes = appointment.DurationMinutes,
                Status = appointment.Status,
                AppointmentType = appointment.AppointmentType,
                Location = appointment.Location,
                ProviderId = appointment.ProviderId,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt,
                SourceLeadId = appointment.SourceLeadId
            };
        }
    }
}
