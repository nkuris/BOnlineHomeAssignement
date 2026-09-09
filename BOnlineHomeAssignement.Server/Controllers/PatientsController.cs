using BOnlineHomeAssignement.Server.Application.DTOs.Patient;
using BOnlineHomeAssignement.Server.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BOnlineHomeAssignement.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(IPatientService patientService, ILogger<PatientsController> logger)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Get all patients for current tenant</summary>
        [HttpGet]
        public async Task<ActionResult<List<PatientResponseDto>>> GetPatients()
        {
            try
            {
                // TODO: Extract from JWT token in production
                var tenantId = GetTenantIdFromContext();
                var userId = GetUserIdFromContext();
                var userRole = GetUserRoleFromContext();

                var patients = await _patientService.GetPatientsAsync(tenantId, userId, userRole);
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patients");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Get a specific patient by ID</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientResponseDto>> GetPatient(Guid id)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var userId = GetUserIdFromContext();
                var userRole = GetUserRoleFromContext();

                var patient = await _patientService.GetPatientAsync(id, tenantId, userId, userRole);

                if (patient == null)
                    return NotFound(new { error = "Patient not found" });

                return Ok(patient);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to patient {PatientId}", id);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Create a new patient</summary>
        [HttpPost]
        public async Task<ActionResult<PatientResponseDto>> CreatePatient([FromBody] CreatePatientDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = GetTenantIdFromContext();
                var userId = GetUserIdFromContext();

                var created = await _patientService.CreatePatientAsync(dto, tenantId, userId);
                return CreatedAtAction(nameof(GetPatient), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating patient");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Update a patient</summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PatientResponseDto>> UpdatePatient(Guid id, [FromBody] UpdatePatientDto dto)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var userId = GetUserIdFromContext();
                var userRole = GetUserRoleFromContext();

                var updated = await _patientService.UpdatePatientAsync(id, dto, tenantId, userId, userRole);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized update attempt for patient {PatientId}", id);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update error for patient {PatientId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Delete a patient (soft delete)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var userId = GetUserIdFromContext();
                var userRole = GetUserRoleFromContext();

                await _patientService.DeletePatientAsync(id, tenantId, userId, userRole);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt for patient {PatientId}", id);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Delete error for patient {PatientId}", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Add user to patient's care team</summary>
        [HttpPost("{patientId}/care-team/{userId}")]
        public async Task<IActionResult> AddToCareTeam(Guid patientId, Guid userId)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                await _patientService.AddToCareTeamAsync(patientId, userId, tenantId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to care team");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Remove user from patient's care team</summary>
        [HttpDelete("{patientId}/care-team/{userId}")]
        public async Task<IActionResult> RemoveFromCareTeam(Guid patientId, Guid userId)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                await _patientService.RemoveFromCareTeamAsync(patientId, userId, tenantId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from care team");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ==================== HELPER METHODS ====================
        // TODO: Replace with actual JWT token extraction in production

        private Guid GetTenantIdFromContext()
        {
            // For now, return a test GUID. In production, extract from JWT claims
            return Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        }

        private Guid GetUserIdFromContext()
        {
            // For now, return a test GUID. In production, extract from JWT claims
            return Guid.Parse("12345678-1234-1234-1234-123456789000");
        }

        private string GetUserRoleFromContext()
        {
            // For now, return "Doctor". In production, extract from JWT claims
            return "Doctor";
        }
    }
}
