namespace BOnlineHomeAssignement.Server.Application.DTOs.Patient
{
    /// <summary>Request DTO for updating a patient</summary>
    public class UpdatePatientDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid? PrimaryDocId { get; set; }
        public Guid? AssignedNurseId { get; set; }
        public bool? IsActive { get; set; }
    }
}
