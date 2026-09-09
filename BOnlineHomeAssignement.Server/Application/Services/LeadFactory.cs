using System;
using System.Collections.Generic;
using System.Text.Json;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead.Supplier;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Domain.Enums;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Factory for creating Lead entities from various supplier sources.
    /// Handles payload mapping, validation, and metadata extraction based on source.
    /// </summary>
    public class LeadFactory
    {
        /// <summary>
        /// Create a Lead from a website form submission.
        /// </summary>
        public static Lead CreateFromWebsite(Guid tenantId, WebsiteLeadDto dto)
        {
            var lead = new Lead
            {
                TenantId = tenantId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                LeadSource = LeadSource.Website,
                Source = LeadSource.Website.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Store supplier-specific data as JSON
            if (ShouldStorePayload(LeadSource.Website))
            {
                var payload = new
                {
                    message = dto.Message,
                    utmSource = dto.UtmSource,
                    utmMedium = dto.UtmMedium,
                    utmCampaign = dto.UtmCampaign,
                    utmContent = dto.UtmContent,
                    utmTerm = dto.UtmTerm,
                    referrerUrl = dto.ReferrerUrl,
                    userAgent = dto.UserAgent
                };
                lead.SupplierPayload = JsonSerializer.Serialize(payload);
            }

            return lead;
        }

        /// <summary>
        /// Create a Lead from a mobile app submission.
        /// </summary>
        public static Lead CreateFromMobile(Guid tenantId, MobileLeadDto dto)
        {
            var lead = new Lead
            {
                TenantId = tenantId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                LeadSource = LeadSource.Mobile,
                Source = LeadSource.Mobile.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Store mobile-specific data as JSON
            if (ShouldStorePayload(LeadSource.Mobile))
            {
                var payload = new
                {
                    deviceId = dto.DeviceId,
                    appVersion = dto.AppVersion,
                    osVersion = dto.OSVersion,
                    latitude = dto.Latitude,
                    longitude = dto.Longitude
                };
                lead.SupplierPayload = JsonSerializer.Serialize(payload);
            }

            return lead;
        }

        /// <summary>
        /// Create a Lead from a CRM system import.
        /// </summary>
        public static Lead CreateFromCRM(Guid tenantId, CRMLeadDto dto)
        {
            var lead = new Lead
            {
                TenantId = tenantId,
                Name = $"{dto.FirstName} {dto.LastName}".Trim(),
                Email = dto.Email,
                Phone = dto.Phone,
                LeadSource = LeadSource.CRM,
                Source = LeadSource.CRM.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Store CRM-specific data as JSON
            if (ShouldStorePayload(LeadSource.CRM))
            {
                var payload = new
                {
                    firstName = dto.FirstName,
                    lastName = dto.LastName,
                    company = dto.Company,
                    jobTitle = dto.JobTitle,
                    externalCRMId = dto.ExternalCRMId,
                    crmSource = dto.CRMSource,
                    owner = dto.Owner,
                    stage = dto.Stage,
                    rating = dto.Rating
                };
                lead.SupplierPayload = JsonSerializer.Serialize(payload);
            }

            return lead;
        }

        /// <summary>
        /// Create a Lead from a referral (by existing patient/lead).
        /// </summary>
        public static Lead CreateFromReferral(Guid tenantId, ReferralLeadDto dto)
        {
            var lead = new Lead
            {
                TenantId = tenantId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                LeadSource = LeadSource.Referral,
                Source = LeadSource.Referral.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Store referral-specific data as JSON
            if (ShouldStorePayload(LeadSource.Referral))
            {
                var payload = new
                {
                    referrerPatientId = dto.ReferrerPatientId,
                    referralNotes = dto.ReferralNotes
                };
                lead.SupplierPayload = JsonSerializer.Serialize(payload);
            }

            return lead;
        }

        /// <summary>
        /// Create a Lead from generic API/webhook submission.
        /// </summary>
        public static Lead CreateFromAPI(Guid tenantId, APILeadDto dto)
        {
            var lead = new Lead
            {
                TenantId = tenantId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                LeadSource = LeadSource.ThirdPartyAPI,
                Source = LeadSource.ThirdPartyAPI.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Store raw API payload
            if (ShouldStorePayload(LeadSource.ThirdPartyAPI) && !string.IsNullOrWhiteSpace(dto.RawPayload))
            {
                var payload = new
                {
                    externalId = dto.ExternalId,
                    rawPayload = dto.RawPayload
                };
                lead.SupplierPayload = JsonSerializer.Serialize(payload);
            }

            return lead;
        }

        /// <summary>
        /// Create a Lead from manual entry (no supplier-specific data).
        /// </summary>
        public static Lead CreateManual(Guid tenantId, CreateLeadDto dto)
        {
            return new Lead
            {
                TenantId = tenantId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                LeadSource = LeadSource.Manual,
                Source = dto.Source ?? LeadSource.Manual.ToString(),
                CreatedAt = DateTime.UtcNow
                // SupplierPayload intentionally left null for manual entries
            };
        }

        /// <summary>
        /// Extract typed payload from a Lead entity (if present).
        /// </summary>
        public static T? ExtractPayload<T>(Lead lead) where T : class
        {
            if (string.IsNullOrWhiteSpace(lead.SupplierPayload))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(lead.SupplierPayload);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Check if a lead source should store the raw payload.
        /// </summary>
        private static bool ShouldStorePayload(LeadSource source)
        {
            var config = LeadSourceConfigs.Get(source);
            return config?.StoreRawPayload ?? false;
        }
    }
}
