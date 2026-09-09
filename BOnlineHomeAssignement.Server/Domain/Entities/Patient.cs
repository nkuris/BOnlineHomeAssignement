using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class Patient
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ==================== TENANT ISOLATION ====================
        [Required]
        public Guid TenantId { get; set; }

        // ==================== PATIENT INFORMATION ====================
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        // ==================== PATIENT PRIVACY & ACCESS CONTROL ====================
        /// <summary>Primary doctor responsible for this patient (can see all data)</summary>
        public Guid? PrimaryDocId { get; set; }

        /// <summary>Primary nurse assigned to this patient</summary>
        public Guid? AssignedNurseId { get; set; }

        /// <summary>Care team members who can access this patient's data</summary>
        public List<Guid> CareTeamIds { get; set; } = new();

        // ==================== METADATA ====================
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
