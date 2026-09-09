using BOnlineHomeAssignement.Server.Application.DTOs.Patient;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>Patient service implementing business logic with security</summary>
    public class PatientService : IPatientService
    {
        private readonly IRepository<Patient> _patientRepository;
        private readonly ILogger<PatientService> _logger;

        // User roles for authorization
        private const string AdminRole = "Admin";
        private const string DoctorRole = "Doctor";
        private const string NurseRole = "Nurse";
        private const string ReceptionistRole = "Receptionist";

        public PatientService(
            IRepository<Patient> patientRepository,
            ILogger<PatientService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Get all patients for the current user's tenant with access control</summary>
        public async Task<List<PatientResponseDto>> GetPatientsAsync(Guid tenantId, Guid userId, string userRole)
        {
            _logger.LogInformation("GetPatients requested by User {UserId} with role {UserRole} for Tenant {TenantId}", userId, userRole, tenantId);

            // SECURITY LAYER 1: Filter by tenant
            var baseQuery = (await _patientRepository.FindAsync(p => p.TenantId == tenantId)).ToList();

            // SECURITY LAYER 2: Filter by patient privacy (access control)
            var accessiblePatients = userRole switch
            {
                AdminRole => baseQuery,  // Admin sees all patients in tenant
                DoctorRole => baseQuery, // Doctor sees all patients in tenant
                NurseRole => baseQuery.Where(p =>
                    p.CareTeamIds.Contains(userId) ||           // On care team
                    p.AssignedNurseId == userId                 // Assigned nurse
                ).ToList(),
                ReceptionistRole => baseQuery.Where(p =>
                    p.CareTeamIds.Contains(userId)              // Only if explicitly added to team
                ).ToList(),
                _ => new List<Patient>()
            };

            _logger.LogInformation("GetPatients: User {UserId} has access to {Count} patients (Tenant: {TenantId})", 
                userId, accessiblePatients.Count, tenantId);

            return accessiblePatients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => MapToResponseDto(p, userRole))
                .ToList();
        }

        /// <summary>Get a specific patient with access control</summary>
        public async Task<PatientResponseDto?> GetPatientAsync(Guid patientId, Guid tenantId, Guid userId, string userRole)
        {
            _logger.LogInformation("GetPatient {PatientId} requested by User {UserId} (role: {UserRole}) for Tenant {TenantId}", 
                patientId, userId, userRole, tenantId);

            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
            {
                _logger.LogWarning("Patient {PatientId} not found", patientId);
                return null;
            }

            // SECURITY CHECK 1: Tenant isolation
            if (patient.TenantId != tenantId)
            {
                _logger.LogWarning("Access denied: Patient {PatientId} belongs to Tenant {PatientTenant}, user is in {UserTenant}", 
                    patientId, patient.TenantId, tenantId);
                throw new UnauthorizedAccessException("You do not have access to this patient");
            }

            // SECURITY CHECK 2: Patient privacy (access control)
            if (!HasAccessToPatient(patient, userId, userRole))
            {
                _logger.LogWarning("Access denied: User {UserId} (role: {UserRole}) cannot access patient {PatientId}", 
                    userId, userRole, patientId);
                throw new UnauthorizedAccessException($"You do not have access to patient {patientId}");
            }

            return MapToResponseDto(patient, userRole);
        }

        /// <summary>Create a new patient</summary>
        public async Task<PatientResponseDto> CreatePatientAsync(CreatePatientDto dto, Guid tenantId, Guid userId)
        {
            _logger.LogInformation("CreatePatient requested by User {UserId} for Tenant {TenantId}", userId, tenantId);

            if (string.IsNullOrWhiteSpace(dto.FirstName))
                throw new ArgumentException("FirstName is required");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                throw new ArgumentException("LastName is required");

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,                          // ← Automatic tenant assignment
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                DateOfBirth = dto.DateOfBirth,
                Email = dto.Email?.Trim(),
                Phone = dto.Phone?.Trim(),
                PrimaryDocId = dto.PrimaryDocId,
                AssignedNurseId = dto.AssignedNurseId,
                CareTeamIds = new List<Guid> { userId },      // ← Creator added to care team
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _patientRepository.AddAsync(patient);
            _logger.LogInformation("Patient {PatientId} created successfully for Tenant {TenantId}", created.Id, tenantId);

            return MapToResponseDto(created, "Doctor");
        }

        /// <summary>Update a patient with access control</summary>
        public async Task<PatientResponseDto> UpdatePatientAsync(
            Guid patientId, UpdatePatientDto dto, Guid tenantId, Guid userId, string userRole)
        {
            _logger.LogInformation("UpdatePatient {PatientId} requested by User {UserId} (role: {UserRole}) for Tenant {TenantId}", 
                patientId, userId, userRole, tenantId);

            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
                throw new InvalidOperationException($"Patient {patientId} not found");

            // SECURITY CHECK 1: Tenant isolation
            if (patient.TenantId != tenantId)
                throw new UnauthorizedAccessException("You do not have access to this patient");

            // SECURITY CHECK 2: Only doctors and admins can edit
            if (userRole != AdminRole && userRole != DoctorRole)
                throw new UnauthorizedAccessException("Only doctors can edit patient information");

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                patient.FirstName = dto.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                patient.LastName = dto.LastName.Trim();

            if (dto.DateOfBirth.HasValue)
                patient.DateOfBirth = dto.DateOfBirth;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                patient.Email = dto.Email.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                patient.Phone = dto.Phone.Trim();

            if (dto.PrimaryDocId.HasValue)
                patient.PrimaryDocId = dto.PrimaryDocId;

            if (dto.AssignedNurseId.HasValue)
                patient.AssignedNurseId = dto.AssignedNurseId;

            if (dto.IsActive.HasValue)
                patient.IsActive = dto.IsActive.Value;

            patient.UpdatedAt = DateTime.UtcNow;

            await _patientRepository.UpdateAsync(patient);
            _logger.LogInformation("Patient {PatientId} updated successfully for Tenant {TenantId}", patientId, tenantId);

            return MapToResponseDto(patient, userRole);
        }

        /// <summary>Delete a patient (soft delete via IsActive flag)</summary>
        public async Task DeletePatientAsync(Guid patientId, Guid tenantId, Guid userId, string userRole)
        {
            _logger.LogInformation("DeletePatient {PatientId} requested by User {UserId} (role: {UserRole}) for Tenant {TenantId}", 
                patientId, userId, userRole, tenantId);

            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
                throw new InvalidOperationException($"Patient {patientId} not found");

            // SECURITY CHECK 1: Tenant isolation
            if (patient.TenantId != tenantId)
                throw new UnauthorizedAccessException("You do not have access to this patient");

            // SECURITY CHECK 2: Only doctors and admins can delete
            if (userRole != AdminRole && userRole != DoctorRole)
                throw new UnauthorizedAccessException("Only doctors can delete patients");

            // Soft delete
            patient.IsActive = false;
            patient.UpdatedAt = DateTime.UtcNow;

            await _patientRepository.UpdateAsync(patient);
            _logger.LogInformation("Patient {PatientId} deleted (soft delete) for Tenant {TenantId}", patientId, tenantId);
        }

        /// <summary>Add a user to patient's care team</summary>
        public async Task AddToCareTeamAsync(Guid patientId, Guid userId, Guid tenantId)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
                throw new InvalidOperationException($"Patient {patientId} not found");

            if (patient.TenantId != tenantId)
                throw new UnauthorizedAccessException("Access denied");

            if (!patient.CareTeamIds.Contains(userId))
            {
                patient.CareTeamIds.Add(userId);
                patient.UpdatedAt = DateTime.UtcNow;
                await _patientRepository.UpdateAsync(patient);
                _logger.LogInformation("User {UserId} added to care team for Patient {PatientId}", userId, patientId);
            }
        }

        /// <summary>Remove a user from patient's care team</summary>
        public async Task RemoveFromCareTeamAsync(Guid patientId, Guid userId, Guid tenantId)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
                throw new InvalidOperationException($"Patient {patientId} not found");

            if (patient.TenantId != tenantId)
                throw new UnauthorizedAccessException("Access denied");

            if (patient.CareTeamIds.Contains(userId))
            {
                patient.CareTeamIds.Remove(userId);
                patient.UpdatedAt = DateTime.UtcNow;
                await _patientRepository.UpdateAsync(patient);
                _logger.LogInformation("User {UserId} removed from care team for Patient {PatientId}", userId, patientId);
            }
        }

        // ==================== HELPER METHODS ====================

        /// <summary>Check if user has access to patient based on role and care team</summary>
        private bool HasAccessToPatient(Patient patient, Guid userId, string userRole)
        {
            return userRole switch
            {
                AdminRole => true,                           // Admin sees all
                DoctorRole => true,                          // Doctor sees all in tenant
                NurseRole => 
                    patient.CareTeamIds.Contains(userId) ||  // On care team OR
                    patient.AssignedNurseId == userId,       // Assigned nurse
                ReceptionistRole => 
                    patient.CareTeamIds.Contains(userId),    // Only if explicitly on team
                _ => false
            };
        }

        /// <summary>Map Patient entity to DTO with role-based data masking</summary>
        private PatientResponseDto MapToResponseDto(Patient patient, string userRole)
        {
            var dto = new PatientResponseDto
            {
                Id = patient.Id,
                TenantId = patient.TenantId,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Email = patient.Email,
                Phone = patient.Phone,
                PrimaryDocId = patient.PrimaryDocId,
                AssignedNurseId = patient.AssignedNurseId,
                CareTeamIds = patient.CareTeamIds,
                IsActive = patient.IsActive,
                CreatedAt = patient.CreatedAt,
                UpdatedAt = patient.UpdatedAt
            };

            // DATA MASKING: Receptionist doesn't see sensitive fields
            if (userRole == ReceptionistRole)
            {
                dto.DateOfBirth = null;
                dto.PrimaryDocId = null;
                dto.AssignedNurseId = null;
                dto.CareTeamIds = new();
            }

            return dto;
        }
    }
}
