using System;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string EntityName { get; set; } = string.Empty;

        public Guid? EntityId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g., Created, Updated, Deleted

        public string? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // JSON or textual description of changes
        public string? Changes { get; set; }
    }
}
