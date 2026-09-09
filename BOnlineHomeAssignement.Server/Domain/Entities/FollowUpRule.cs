using System;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class FollowUpRule
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Interval in days
        public int IntervalDays { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
