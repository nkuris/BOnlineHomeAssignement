using BOnlineHomeAssignement.Server.Application.DTOs.Patient;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>Patient service interface for business logic</summary>
    public interface IPatientService
    {
        /// <summary>Get all patients for the current user's tenant</summary>
        Task<List<PatientResponseDto>> GetPatientsAsync(Guid tenantId, Guid userId, string userRole);

        /// <summary>Get a specific patient with access control</summary>
        Task<PatientResponseDto?> GetPatientAsync(Guid patientId, Guid tenantId, Guid userId, string userRole);

        /// <summary>Create a new patient</summary>
        Task<PatientResponseDto> CreatePatientAsync(CreatePatientDto dto, Guid tenantId, Guid userId);

        /// <summary>Update a patient</summary>
        Task<PatientResponseDto> UpdatePatientAsync(Guid patientId, UpdatePatientDto dto, Guid tenantId, Guid userId, string userRole);

        /// <summary>Delete a patient</summary>
        Task DeletePatientAsync(Guid patientId, Guid tenantId, Guid userId, string userRole);

        /// <summary>Add user to patient's care team</summary>
        Task AddToCareTeamAsync(Guid patientId, Guid userId, Guid tenantId);

        /// <summary>Remove user from patient's care team</summary>
        Task RemoveFromCareTeamAsync(Guid patientId, Guid userId, Guid tenantId);
    }
}
