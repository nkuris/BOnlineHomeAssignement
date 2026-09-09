using System;
using System.ComponentModel.DataAnnotations;
using BOnlineHomeAssignement.Server.Domain.Enums;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class Lead
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ==================== TENANT ISOLATION ====================
        [Required]
        public Guid TenantId { get; set; }

        // ==================== LEAD INFORMATION ====================
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        // ==================== LEAD SOURCE & SUPPLIER DATA ====================
        /// <summary>
        /// Identifies the supplier/source of this lead (Website, CRM, Mobile, etc.)
        /// Used to determine field mappings and validation rules
        /// </summary>
        [Required]
        public LeadSource LeadSource { get; set; } = LeadSource.Manual;

        /// <summary>
        /// Legacy field - kept for backward compatibility
        /// Use LeadSource enum for new code
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Raw supplier payload stored as JSON
        /// Allows storing supplier-specific fields without modifying schema
        /// Example: { "companyName": "Acme", "utmSource": "google", "surveyResponse": {...} }
        /// </summary>
        public string? SupplierPayload { get; set; }

        // ==================== METADATA ====================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
