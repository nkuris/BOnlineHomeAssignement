namespace BOnlineHomeAssignement.Server.Application.DTOs.Patient
{
    /// <summary>Request DTO for creating a new patient</summary>
    public class CreatePatientDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid? PrimaryDocId { get; set; }
        public Guid? AssignedNurseId { get; set; }
    }
}
