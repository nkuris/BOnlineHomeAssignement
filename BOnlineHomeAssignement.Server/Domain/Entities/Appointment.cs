using System;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    /// <summary>
    /// Represents a scheduled appointment between a patient and provider.
    /// Links patient records to specific service visits, treatments, or consultations.
    /// </summary>
    public class Appointment
    {
        /// <summary>Unique identifier for the appointment.</summary>
        public Guid Id { get; set; }

        /// <summary>Tenant this appointment belongs to (multi-tenant isolation).</summary>
        public Guid TenantId { get; set; }

        /// <summary>Reference to the patient for this appointment.</summary>
        public Guid PatientId { get; set; }

        /// <summary>Navigation property to Patient entity.</summary>
        public Patient? Patient { get; set; }

        /// <summary>Reference to the enrollment this appointment is for (optional).</summary>
        public Guid? EnrollmentId { get; set; }

        /// <summary>Navigation property to Enrollment entity.</summary>
        public Enrollment? Enrollment { get; set; }

        /// <summary>
        /// Title/subject of the appointment (e.g., "Initial Consultation", "Follow-up Visit").
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the appointment (reason for visit, notes, etc.).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Scheduled start date and time for the appointment.
        /// </summary>
        public DateTime ScheduledStart { get; set; }

        /// <summary>
        /// Scheduled end date and time for the appointment.
        /// Derived from ScheduledStart + Duration if not explicitly set.
        /// </summary>
        public DateTime? ScheduledEnd { get; set; }

        /// <summary>
        /// Expected duration in minutes (e.g., 30, 60, 90).
        /// Used to calculate ScheduledEnd if not explicitly provided.
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Status of the appointment: Scheduled, Confirmed, In Progress, Completed, Cancelled, No-Show.
        /// </summary>
        public string Status { get; set; } = "Scheduled"; // "Scheduled", "Confirmed", "Completed", "Cancelled", "No-Show"

        /// <summary>
        /// Type of appointment (e.g., "Virtual", "In-Person", "Phone").
        /// </summary>
        public string? AppointmentType { get; set; }

        /// <summary>
        /// Location or meeting details (address, room number, Zoom link, phone number, etc.).
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Provider/staff member assigned to this appointment (e.g., doctor name, staff ID).
        /// Optional: can be filled during scheduling.
        /// </summary>
        public string? ProviderId { get; set; }

        /// <summary>
        /// Notes from the appointment (outcome, observations, next steps).
        /// Filled after appointment occurs.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Timestamp when the appointment record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the appointment was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Track if appointment was created from a Lead conversion.
        /// Useful for attribution and reporting.
        /// </summary>
        public Guid? SourceLeadId { get; set; }

        /// <summary>
        /// Custom data/metadata stored as JSON for extensibility.
        /// Supports appointment-specific tracking or provider-specific fields.
        /// </summary>
        public string? Metadata { get; set; }
    }
}
