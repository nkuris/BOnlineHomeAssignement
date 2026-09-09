using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead.Supplier;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Domain.Enums;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    public class LeadService : ILeadService
    {
        private readonly IRepository<Lead> _repository;

        public LeadService(IRepository<Lead> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<LeadResponseDto>> GetLeadsAsync(Guid tenantId)
        {
            var leads = await _repository.FindAsync(l => l.TenantId == tenantId);
            return leads.OrderByDescending(l => l.CreatedAt).Select(MapToResponseDto);
        }

        public async Task<LeadResponseDto?> GetLeadAsync(Guid tenantId, Guid leadId)
        {
            var lead = await _repository.FirstOrDefaultAsync(
                l => l.Id == leadId && l.TenantId == tenantId);

            return lead != null ? MapToResponseDto(lead) : null;
        }

        public async Task<LeadResponseDto> CreateLeadAsync(Guid tenantId, CreateLeadDto dto)
        {
            var lead = LeadFactory.CreateManual(tenantId, dto);

            await _repository.AddAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        /// <summary>
        /// Create a lead from a website form submission.
        /// </summary>
        public async Task<LeadResponseDto> CreateWebsiteLeadAsync(Guid tenantId, WebsiteLeadDto dto)
        {
            var lead = LeadFactory.CreateFromWebsite(tenantId, dto);

            await _repository.AddAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        /// <summary>
        /// Create a lead from a mobile app submission.
        /// </summary>
        public async Task<LeadResponseDto> CreateMobileLeadAsync(Guid tenantId, MobileLeadDto dto)
        {
            var lead = LeadFactory.CreateFromMobile(tenantId, dto);

            await _repository.AddAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        /// <summary>
        /// Create a lead from CRM import.
        /// </summary>
        public async Task<LeadResponseDto> CreateCRMLeadAsync(Guid tenantId, CRMLeadDto dto)
        {
            var lead = LeadFactory.CreateFromCRM(tenantId, dto);

            await _repository.AddAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        /// <summary>
        /// Create a referral lead (from existing patient).
        /// </summary>
        public async Task<LeadResponseDto> CreateReferralLeadAsync(Guid tenantId, ReferralLeadDto dto)
        {
            var lead = LeadFactory.CreateFromReferral(tenantId, dto);

            await _repository.AddAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        /// <summary>
        /// Create a lead from generic API/webhook.
        /// </summary>
        public async Task<LeadResponseDto> CreateAPILeadAsync(Guid tenantId, APILeadDto dto)
        {
            var lead = LeadFactory.CreateFromAPI(tenantId, dto);

            await _repository.AddAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        public async Task<LeadResponseDto> UpdateLeadAsync(Guid tenantId, Guid leadId, UpdateLeadDto dto)
        {
            var lead = await _repository.FirstOrDefaultAsync(
                l => l.Id == leadId && l.TenantId == tenantId);

            if (lead == null)
                throw new KeyNotFoundException($"Lead {leadId} not found in tenant {tenantId}");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                lead.Name = dto.Name;

            if (dto.Email != null)
                lead.Email = dto.Email;

            if (dto.Phone != null)
                lead.Phone = dto.Phone;

            if (dto.Source != null)
                lead.Source = dto.Source;

            await _repository.UpdateAsync(lead);
            await _repository.SaveChangesAsync();

            return MapToResponseDto(lead);
        }

        public async Task DeleteLeadAsync(Guid tenantId, Guid leadId)
        {
            var lead = await _repository.FirstOrDefaultAsync(
                l => l.Id == leadId && l.TenantId == tenantId);

            if (lead == null)
                throw new KeyNotFoundException($"Lead {leadId} not found in tenant {tenantId}");

            await _repository.DeleteAsync(lead);
            await _repository.SaveChangesAsync();
        }

        private static LeadResponseDto MapToResponseDto(Lead lead)
        {
            return new LeadResponseDto
            {
                Id = lead.Id,
                TenantId = lead.TenantId,
                Name = lead.Name,
                Email = lead.Email,
                Phone = lead.Phone,
                CreatedAt = lead.CreatedAt,
                LeadSource = lead.LeadSource,
                Source = lead.Source
            };
        }

        /// <summary>
        /// Map Lead to detailed response including raw supplier payload.
        /// </summary>
        private static LeadDetailedResponseDto MapToDetailedResponseDto(Lead lead)
        {
            return new LeadDetailedResponseDto
            {
                Id = lead.Id,
                TenantId = lead.TenantId,
                Name = lead.Name,
                Email = lead.Email,
                Phone = lead.Phone,
                CreatedAt = lead.CreatedAt,
                LeadSource = lead.LeadSource,
                Source = lead.Source,
                SupplierPayload = lead.SupplierPayload
            };
        }
    }
}

