using System;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Application.DTOs.Conversion;
using BOnlineHomeAssignement.Server.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BOnlineHomeAssignement.Server.Controllers
{
    /// <summary>
    /// API endpoints for converting leads to patients.
    /// Handles single and bulk conversions, readiness checks, and conversion history.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConversionController : ControllerBase
    {
        private readonly ILeadConversionService _conversionService;
        private readonly ILogger<ConversionController> _logger;

        public ConversionController(
            ILeadConversionService conversionService,
            ILogger<ConversionController> logger)
        {
            _conversionService = conversionService;
            _logger = logger;
        }

        /// <summary>
        /// Check if a lead is ready to be converted to a patient.
        /// Validates lead data and checks for duplicate patients.
        /// </summary>
        /// <param name="tenantId">Tenant ID (from header or claims).</param>
        /// <param name="leadId">Lead ID to check.</param>
        /// <returns>Readiness status with any issues or conflicts.</returns>
        [HttpGet("{leadId}/readiness")]
        public async Task<ActionResult<ConversionReadinessDto>> CheckConversionReadiness(
            [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
            [FromRoute] Guid leadId)
        {
            if (!Guid.TryParse(tenantIdHeader, out var tenantId))
                return BadRequest("Invalid tenant ID in header.");

            try
            {
                var readiness = await _conversionService.CheckConversionReadinessAsync(tenantId, leadId);
                return Ok(readiness);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking conversion readiness for lead {leadId}");
                return StatusCode(500, new { message = "Error checking conversion readiness", error = ex.Message });
            }
        }

        /// <summary>
        /// Convert a single lead to a patient.
        /// Performs validation, deduplication, and patient creation in a single operation.
        /// </summary>
        /// <param name="tenantId">Tenant ID (from header or claims).</param>
        /// <param name="request">Conversion request with lead ID and optional flags.</param>
        /// <returns>Conversion result with patient ID if successful.</returns>
        [HttpPost("convert")]
        public async Task<ActionResult<ConversionResultDto>> ConvertLead(
            [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
            [FromBody] ConvertLeadRequestDto request)
        {
            if (!Guid.TryParse(tenantIdHeader, out var tenantId))
                return BadRequest("Invalid tenant ID in header.");

            if (request.LeadId == Guid.Empty)
                return BadRequest("LeadId is required.");

            try
            {
                var result = await _conversionService.ConvertLeadToPatientAsync(tenantId, request.LeadId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                _logger.LogInformation($"Lead {request.LeadId} successfully converted to patient {result.PatientId}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting lead {request.LeadId}");
                return StatusCode(500, new ConversionResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = $"Conversion failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Bulk convert multiple leads to patients.
        /// Converts multiple leads in a single operation, with option to continue on errors.
        /// </summary>
        /// <param name="tenantId">Tenant ID (from header or claims).</param>
        /// <param name="request">Bulk conversion request with lead IDs list.</param>
        /// <returns>Bulk conversion result with success/failure counts and details.</returns>
        [HttpPost("convert/bulk")]
        public async Task<ActionResult<BulkConversionResultDto>> BulkConvertLeads(
            [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
            [FromBody] BulkConvertLeadsRequestDto request)
        {
            if (!Guid.TryParse(tenantIdHeader, out var tenantId))
                return BadRequest("Invalid tenant ID in header.");

            if (request.LeadIds == null || request.LeadIds.Count == 0)
                return BadRequest("LeadIds list is required and must not be empty.");

            try
            {
                var result = await _conversionService.ConvertLeadsToPatientsBulkAsync(
                    tenantId,
                    request.LeadIds,
                    ignoreErrors: request.IgnoreErrors);

                _logger.LogInformation(
                    $"Bulk conversion completed: {result.SuccessCount} successful, {result.FailureCount} failed");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during bulk conversion of {request.LeadIds.Count} leads");
                return StatusCode(500, new BulkConversionResultDto
                {
                    TotalRequested = request.LeadIds.Count,
                    FailedLeads = new() { [Guid.Empty] = ex.Message }
                });
            }
        }

        /// <summary>
        /// Get conversion history for a lead.
        /// Shows if/when the lead was converted to a patient.
        /// </summary>
        /// <param name="tenantId">Tenant ID (from header or claims).</param>
        /// <param name="leadId">Lead ID to check history for.</param>
        /// <returns>Conversion history if the lead has been converted, or 404 if not converted.</returns>
        [HttpGet("{leadId}/history")]
        public async Task<ActionResult<ConversionHistoryDto>> GetConversionHistory(
            [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
            [FromRoute] Guid leadId)
        {
            if (!Guid.TryParse(tenantIdHeader, out var tenantId))
                return BadRequest("Invalid tenant ID in header.");

            try
            {
                var history = await _conversionService.GetConversionHistoryAsync(tenantId, leadId);

                if (history == null)
                    return NotFound(new { message = "Lead has not been converted to a patient" });

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving conversion history for lead {leadId}");
                return StatusCode(500, new { message = "Error retrieving conversion history", error = ex.Message });
            }
        }
    }
}
