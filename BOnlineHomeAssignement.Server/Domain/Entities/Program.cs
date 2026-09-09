using System;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    // Domain 'Program' entity (e.g., care program). Named Program in the
    // Domain.Entities namespace to avoid colliding with the application
    // entrypoint which lives at the root namespace.
    public class Program
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
