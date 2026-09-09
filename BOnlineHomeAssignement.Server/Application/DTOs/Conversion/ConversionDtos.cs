using System;
using System.Collections.Generic;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Conversion
{
    /// <summary>Result of a single lead conversion attempt.</summary>
    public class ConversionResultDto
    {
        public bool IsSuccess { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public string? EnrollmentProgramName { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public static ConversionResultDto Success(Guid patientId, Guid? enrollmentId = null, string programName = "", string message = "Conversion successful")
            => new() { IsSuccess = true, PatientId = patientId, EnrollmentId = enrollmentId, EnrollmentProgramName = programName, Message = message };

        public static ConversionResultDto Failure(string errorMessage)
            => new() { IsSuccess = false, ErrorMessage = errorMessage };
    }

    /// <summary>Result of a bulk conversion operation.</summary>
    public class BulkConversionResultDto
    {
        public int TotalRequested { get; set; }
        public List<Guid> SuccessfulConversions { get; set; } = new();
        public Dictionary<Guid, string> FailedLeads { get; set; } = new();
        public bool IsPartialSuccess { get; set; }

        public int SuccessCount => SuccessfulConversions.Count;
        public int FailureCount => FailedLeads.Count;
    }

    /// <summary>Readiness status for converting a lead to patient.</summary>
    public class ConversionReadinessDto
    {
        public bool IsReady { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public Guid? PotentialDuplicatePatientId { get; set; }
        public int DuplicateConfidence { get; set; } // 0-100
    }

    /// <summary>History of a lead's conversion to patient.</summary>
    public class ConversionHistoryDto
    {
        public Guid LeadId { get; set; }
        public Guid PatientId { get; set; }
        public DateTime ConvertedAt { get; set; }
        public string ConversionDetails { get; set; } = string.Empty;
    }

    /// <summary>Request to convert a lead to patient.</summary>
    public class ConvertLeadRequestDto
    {
        /// <summary>The lead ID to convert.</summary>
        public Guid LeadId { get; set; }

        /// <summary>
        /// Optional: If conversion finds a duplicate patient, whether to force conversion anyway.
        /// Default false: duplicate detection fails conversion.
        /// </summary>
        public bool IgnoreDuplicateWarning { get; set; } = false;

        /// <summary>
        /// Optional: Whether to automatically create an initial appointment for this patient.
        /// If true, an appointment is scheduled based on TenantConfiguration defaults.
        /// </summary>
        public bool CreateInitialAppointment { get; set; } = false;
    }

    /// <summary>Request to bulk convert multiple leads.</summary>
    public class BulkConvertLeadsRequestDto
    {
        /// <summary>List of lead IDs to convert.</summary>
        public List<Guid> LeadIds { get; set; } = new();

        /// <summary>
        /// If true, continue converting even if some leads fail.
        /// If false, stop on first error.
        /// </summary>
        public bool IgnoreErrors { get; set; } = false;

        /// <summary>
        /// Optional: Whether to create initial appointments for converted patients.
        /// </summary>
        public bool CreateInitialAppointments { get; set; } = false;
    }
}
