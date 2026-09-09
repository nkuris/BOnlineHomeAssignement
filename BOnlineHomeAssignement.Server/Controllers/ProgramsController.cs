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
    public class ProgramsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProgramsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all programs.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Domain.Entities.Program>>> GetPrograms()
        {
            var programs = await _context.Programs.ToListAsync();
            return Ok(programs);
        }

        /// <summary>
        /// Get a specific program by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Domain.Entities.Program>> GetProgram(Guid id)
        {
            var program = await _context.Programs.FindAsync(id);

            if (program == null)
                return NotFound();

            return Ok(program);
        }

        /// <summary>
        /// Create a new program.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Domain.Entities.Program>> CreateProgram([FromBody] CreateProgramRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Program name is required");

            var program = new Domain.Entities.Program
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = true
            };

            _context.Programs.Add(program);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
        }

        /// <summary>
        /// Update an existing program.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProgram(Guid id, [FromBody] UpdateProgramRequest request)
        {
            var program = await _context.Programs.FindAsync(id);

            if (program == null)
                return NotFound();

            program.Name = request.Name ?? program.Name;
            program.Description = request.Description ?? program.Description;
            program.StartDate = request.StartDate ?? program.StartDate;
            program.EndDate = request.EndDate ?? program.EndDate;
            if (request.IsActive.HasValue)
                program.IsActive = request.IsActive.Value;

            _context.Programs.Update(program);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a program.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgram(Guid id)
        {
            var program = await _context.Programs.FindAsync(id);

            if (program == null)
                return NotFound();

            _context.Programs.Remove(program);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateProgramRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateProgramRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
