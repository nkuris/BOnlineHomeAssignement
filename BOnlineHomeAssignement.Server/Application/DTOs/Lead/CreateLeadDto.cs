using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Lead
{
    public class CreateLeadDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public string? Source { get; set; }
    }
}
