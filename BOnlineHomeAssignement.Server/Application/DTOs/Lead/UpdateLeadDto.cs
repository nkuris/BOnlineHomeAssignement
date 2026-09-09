using System.ComponentModel.DataAnnotations;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Lead
{
    public class UpdateLeadDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public string? Source { get; set; }
    }
}
