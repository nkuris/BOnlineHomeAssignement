using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead;
using BOnlineHomeAssignement.Server.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BOnlineHomeAssignement.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        /// <summary>
        /// Get all leads for the current tenant.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadResponseDto>>> GetLeads()
        {
            var tenantId = GetTenantIdFromContext();
            var leads = await _leadService.GetLeadsAsync(tenantId);
            return Ok(leads);
        }

        /// <summary>
        /// Get a specific lead by ID within the current tenant.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeadResponseDto>> GetLead(Guid id)
        {
            var tenantId = GetTenantIdFromContext();
            var lead = await _leadService.GetLeadAsync(tenantId, id);

            if (lead == null)
                return NotFound();

            return Ok(lead);
        }

        /// <summary>
        /// Create a new lead for the current tenant.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeadResponseDto>> CreateLead([FromBody] CreateLeadDto dto)
        {
            var tenantId = GetTenantIdFromContext();

            // Clean up DTO: convert empty strings to null to pass validation
            var cleanDto = new CreateLeadDto
            {
                Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email,
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone,
                Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source
            };

            // Re-validate the cleaned DTO
            var validationContext = new ValidationContext(cleanDto);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(cleanDto, validationContext, validationResults, validateAllProperties: true))
            {
                foreach (var error in validationResults)
                {
                    ModelState.AddModelError(string.Empty, error.ErrorMessage);
                }
                return BadRequest(ModelState);
            }

            var lead = await _leadService.CreateLeadAsync(tenantId, cleanDto);
            return CreatedAtAction(nameof(GetLead), new { id = lead.Id }, lead);
        }

        /// <summary>
        /// Update an existing lead in the current tenant.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLead(Guid id, [FromBody] UpdateLeadDto dto)
        {
            var tenantId = GetTenantIdFromContext();

            try
            {
                // Clean up DTO: convert empty strings to null to pass validation
                var cleanDto = new UpdateLeadDto
                {
                    Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name,
                    Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email,
                    Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone,
                    Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source
                };

                // Re-validate the cleaned DTO
                var validationContext = new ValidationContext(cleanDto);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(cleanDto, validationContext, validationResults, validateAllProperties: true))
                {
                    foreach (var error in validationResults)
                    {
                        ModelState.AddModelError(string.Empty, error.ErrorMessage);
                    }
                    return BadRequest(ModelState);
                }

                var lead = await _leadService.UpdateLeadAsync(tenantId, id, cleanDto);
                return Ok(lead);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Delete a lead from the current tenant.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLead(Guid id)
        {
            var tenantId = GetTenantIdFromContext();

            try
            {
                await _leadService.DeleteLeadAsync(tenantId, id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // ==================== PLACEHOLDER HELPERS ====================
        // TODO: Replace with actual JWT/claims-based extraction
        private Guid GetTenantIdFromContext()
        {
            // Placeholder: For development, use a default tenant ID
            // In production, extract from JWT claims or HttpContext
            var tenantIdClaim = User?.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return tenantId;
            }

            // Development default - returns the first tenant's ID if it exists
            // This allows testing without authentication
            var defaultTenantId = Environment.GetEnvironmentVariable("DEFAULT_TENANT_ID");
            if (!string.IsNullOrEmpty(defaultTenantId) && Guid.TryParse(defaultTenantId, out var envTenantId))
            {
                return envTenantId;
            }

            // Last resort: return a consistent development ID
            // In production, this should be properly authenticated
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }
}

