using System;
using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
    public class Enrollment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        public Guid ProgramId { get; set; }
        public Program? Program { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? Status { get; set; }
    }
}
