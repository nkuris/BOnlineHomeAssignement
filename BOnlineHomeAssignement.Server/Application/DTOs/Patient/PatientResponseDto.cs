namespace BOnlineHomeAssignement.Server.Application.DTOs.Patient
{
    /// <summary>Response DTO for patient data (with data masking capability)</summary>
    public class PatientResponseDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid? PrimaryDocId { get; set; }
        public Guid? AssignedNurseId { get; set; }
        public List<Guid> CareTeamIds { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
