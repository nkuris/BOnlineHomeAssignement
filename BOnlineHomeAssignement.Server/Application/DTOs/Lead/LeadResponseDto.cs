using System;
using System.Text.Json.Serialization;
using BOnlineHomeAssignement.Server.Domain.Enums;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Lead
{
    /// <summary>
    /// Standard response DTO for lead information.
    /// Core properties (Name, Email, Phone) are always included.
    /// SupplierPayload is optional and only included if explicitly requested.
    /// </summary>
    public class LeadResponseDto
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Enumerated lead source/supplier type.
        /// </summary>
        [JsonPropertyName("leadSource")]
        public LeadSource LeadSource { get; set; } = LeadSource.Manual;

        /// <summary>
        /// Legacy source field kept for backward compatibility.
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Raw supplier-specific payload (JSON string).
        /// Only included when explicitly requested via API parameter, not in default list responses.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("supplierPayload")]
        public string? SupplierPayload { get; set; }
    }

    /// <summary>
    /// Extended response DTO that always includes supplier payload.
    /// Use when payload inspection is needed (e.g., detailed lead view, migration/export).
    /// </summary>
    public class LeadDetailedResponseDto : LeadResponseDto
    {
        /// <summary>Force inclusion of SupplierPayload in serialization.</summary>
        public new string? SupplierPayload
        {
            get => base.SupplierPayload;
            set => base.SupplierPayload = value;
        }
    }
}

