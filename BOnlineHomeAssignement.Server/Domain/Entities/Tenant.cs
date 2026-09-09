using System;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class Tenant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Hostname { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
