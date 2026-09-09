using System;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class TenantConfiguration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        [Required]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }
    }
}
