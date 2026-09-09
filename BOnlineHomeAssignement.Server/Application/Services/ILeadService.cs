using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    public interface ILeadService
    {
        Task<IEnumerable<LeadResponseDto>> GetLeadsAsync(Guid tenantId);

        Task<LeadResponseDto?> GetLeadAsync(Guid tenantId, Guid leadId);

        Task<LeadResponseDto> CreateLeadAsync(Guid tenantId, CreateLeadDto dto);

        Task<LeadResponseDto> UpdateLeadAsync(Guid tenantId, Guid leadId, UpdateLeadDto dto);

        Task DeleteLeadAsync(Guid tenantId, Guid leadId);
    }
}
