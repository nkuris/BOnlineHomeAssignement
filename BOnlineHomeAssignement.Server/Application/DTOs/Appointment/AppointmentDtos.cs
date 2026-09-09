using System;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Appointment
{
    /// <summary>Response DTO for appointment information.</summary>
    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }
        public int? DurationMinutes { get; set; }
        public string Status { get; set; } = "Scheduled";
        public string? AppointmentType { get; set; }
        public string? Location { get; set; }
        public string? ProviderId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? SourceLeadId { get; set; }
    }

    /// <summary>Request DTO to create an appointment.</summary>
    public class CreateAppointmentDto
    {
        /// <summary>Title/subject of the appointment (required).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed description of the appointment.</summary>
        public string? Description { get; set; }

        /// <summary>Scheduled start time (required).</summary>
        public DateTime ScheduledStart { get; set; }

        /// <summary>Scheduled end time (optional; can be calculated from duration).</summary>
        public DateTime? ScheduledEnd { get; set; }

        /// <summary>Expected duration in minutes.</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>Type of appointment (e.g., "Virtual", "In-Person", "Phone"). Defaults to "In-Person".</summary>
        public string? AppointmentType { get; set; }

        /// <summary>Location or meeting details (address, room, Zoom link, phone, etc.).</summary>
        public string? Location { get; set; }

        /// <summary>Provider/staff member ID assigned to this appointment.</summary>
        public string? ProviderId { get; set; }

        /// <summary>Optional: Enrollment ID if appointment is tied to a specific program enrollment.</summary>
        public Guid? EnrollmentId { get; set; }
    }

    /// <summary>Request DTO to update an appointment.</summary>
    public class UpdateAppointmentDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }
        public int? DurationMinutes { get; set; }
        public string? AppointmentType { get; set; }
        public string? Location { get; set; }
        public string? ProviderId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>DTO representing an available appointment slot (for Phase 3 scheduling).</summary>
    public class AvailableSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes => (int)EndTime.Subtract(StartTime).TotalMinutes;
    }
}
