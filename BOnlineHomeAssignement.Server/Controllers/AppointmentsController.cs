using BOnlineHomeAssignement.Server.Application.DTOs.Appointment;
using BOnlineHomeAssignement.Server.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BOnlineHomeAssignement.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Get all appointments for a specific patient</summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetPatientAppointments(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var appointments = await _appointmentService.GetPatientAppointmentsAsync(tenantId, patientId);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting appointments for patient {patientId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Get all appointments for the tenant with optional date range filtering</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetTenantAppointments(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var appointments = await _appointmentService.GetTenantAppointmentsAsync(tenantId, fromDate, toDate);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant appointments");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Get a specific appointment by ID</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentResponseDto>> GetAppointment(Guid id)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var appointment = await _appointmentService.GetAppointmentAsync(tenantId, id);

                if (appointment == null)
                    return NotFound(new { error = $"Appointment {id} not found" });

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting appointment {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Create a new appointment for a patient</summary>
        [HttpPost("patient/{patientId}")]
        public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment(Guid patientId, CreateAppointmentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = GetTenantIdFromContext();
                var appointment = await _appointmentService.CreateAppointmentAsync(tenantId, patientId, dto);

                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid appointment request for patient {patientId}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating appointment for patient {patientId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Create an appointment from lead conversion (auto-scheduled initial consultation)</summary>
        [HttpPost("from-lead-conversion/{patientId}/{sourceLeadId}")]
        public async Task<ActionResult<AppointmentResponseDto>> CreateAppointmentFromLeadConversion(
            Guid patientId,
            Guid sourceLeadId,
            CreateAppointmentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = GetTenantIdFromContext();
                var appointment = await _appointmentService.CreateAppointmentFromLeadConversionAsync(
                    tenantId, patientId, sourceLeadId, dto);

                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid appointment request for patient {patientId} from lead {sourceLeadId}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating appointment from lead conversion");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Update an existing appointment</summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointment(Guid id, UpdateAppointmentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = GetTenantIdFromContext();
                var appointment = await _appointmentService.UpdateAppointmentAsync(tenantId, id, dto);

                return Ok(appointment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid update request for appointment {id}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating appointment {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Update appointment status (Scheduled, Confirmed, Completed, Cancelled, No-Show)</summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointmentStatus(Guid id, [FromBody] string newStatus)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newStatus))
                    return BadRequest(new { error = "Status cannot be empty" });

                var tenantId = GetTenantIdFromContext();
                var appointment = await _appointmentService.UpdateAppointmentStatusAsync(tenantId, id, newStatus);

                return Ok(appointment);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid status update for appointment {id}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for appointment {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Delete an appointment (soft delete via status change to Cancelled)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(Guid id)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                await _appointmentService.DeleteAppointmentAsync(tenantId, id);

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Cannot delete appointment {id}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting appointment {id}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Get available appointment slots for a provider on a specific date</summary>
        [HttpGet("provider/{providerId}/available-slots")]
        public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(
            string providerId,
            [FromQuery] DateTime date)
        {
            try
            {
                if (date == DateTime.MinValue)
                    return BadRequest(new { error = "Date query parameter is required" });

                var tenantId = GetTenantIdFromContext();
                var slots = await _appointmentService.GetAvailableSlotsAsync(tenantId, providerId, date);

                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available slots for provider {providerId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>Get appointments by enrollment</summary>
        [HttpGet("enrollment/{enrollmentId}")]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetAppointmentsByEnrollment(Guid enrollmentId)
        {
            try
            {
                var tenantId = GetTenantIdFromContext();
                var appointments = await _appointmentService.GetTenantAppointmentsAsync(tenantId);
                var filtered = appointments.Where(a => a.EnrollmentId == enrollmentId);

                return Ok(filtered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting appointments for enrollment {enrollmentId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ==================== HELPER METHODS ====================
        // TODO: Replace with actual JWT token extraction in production

        private Guid GetTenantIdFromContext()
        {
            // Try to get from header first (X-Tenant-Id)
            if (Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
            {
                if (Guid.TryParse(tenantHeader.ToString(), out var tenantId))
                    return tenantId;
            }

            // For now, return a test GUID. In production, extract from JWT claims
            return Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        }
    }
}
