using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Lead.Supplier
{
    /// <summary>
    /// Input DTO for leads coming from website form submissions.
    /// Includes web-specific fields like marketing utm parameters.
    /// </summary>
    public class WebsiteLeadDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public string? Message { get; set; }

        // UTM Tracking
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public string? UtmContent { get; set; }
        public string? UtmTerm { get; set; }

        // Technical
        public string? ReferrerUrl { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Input DTO for leads from mobile applications.
    /// Includes mobile-specific fields like device info.
    /// </summary>
    public class MobileLeadDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        // Mobile-specific
        public string? DeviceId { get; set; }
        public string? AppVersion { get; set; }
        public string? OSVersion { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    /// <summary>
    /// Input DTO for leads imported from CRM systems (Salesforce, HubSpot, etc.)
    /// </summary>
    public class CRMLeadDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public string? Company { get; set; }
        public string? JobTitle { get; set; }

        // CRM-specific
        public string? ExternalCRMId { get; set; }
        public string? CRMSource { get; set; }
        public string? Owner { get; set; }
        public string? Stage { get; set; }
        public string? Rating { get; set; }
    }

    /// <summary>
    /// Input DTO for referral leads (from existing patients/leads).
    /// </summary>
    public class ReferralLeadDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [Required]
        public System.Guid ReferrerPatientId { get; set; }

        public string? ReferralNotes { get; set; }
    }

    /// <summary>
    /// Generic DTO for API/webhook lead submissions with flexible payload.
    /// </summary>
    public class APILeadDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        /// <summary>
        /// Raw JSON payload from external API.
        /// Will be stored in Lead.SupplierPayload to preserve all supplier data.
        /// </summary>
        public string? RawPayload { get; set; }

        /// <summary>
        /// External API identifier (for tracking/syncing)
        /// </summary>
        public string? ExternalId { get; set; }
    }
}
