using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using BOnlineHomeAssignement.Server.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BOnlineHomeAssignement.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TenantsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all tenants.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants()
        {
            var tenants = await _context.Tenants.ToListAsync();
            return Ok(tenants);
        }

        /// <summary>
        /// Get a specific tenant by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenant(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }

        /// <summary>
        /// Create a new tenant.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Tenant name is required");

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Hostname = request.Hostname ?? request.Name.ToLowerInvariant().Replace(" ", "-"),
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
        }

        /// <summary>
        /// Update an existing tenant.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
                return NotFound();

            tenant.Name = request.Name ?? tenant.Name;
            tenant.Hostname = request.Hostname ?? tenant.Hostname;

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a tenant.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
                return NotFound();

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; }
        public string Hostname { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string Name { get; set; }
        public string Hostname { get; set; }
    }
}
