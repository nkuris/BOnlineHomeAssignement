using System;
using System.Collections.Generic;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Application.DTOs.Lead.Supplier;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using BOnlineHomeAssignement.Server.Domain.Enums;

namespace BOnlineHomeAssignement.Server.Infrastructure.Utilities
{
    /// <summary>
    /// Helper class for normalizing and mapping Lead data to Patient fields.
    /// Handles extraction of core patient information from various lead supplier formats.
    /// Provides deduplication and data enrichment utilities.
    /// </summary>
    public static class ConversionHelper
    {
        /// <summary>
        /// Extract core patient-relevant fields from a Lead and its supplier payload.
        /// </summary>
        public static Dictionary<string, object> ExtractPatientFields(Lead lead, LeadSource source)
        {
            var fields = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(lead.Name))
            {
                var nameParts = ParseName(lead.Name);
                fields["FirstName"] = nameParts.FirstName;
                fields["LastName"] = nameParts.LastName;
            }

            if (!string.IsNullOrWhiteSpace(lead.Email))
                fields["Email"] = lead.Email;

            if (!string.IsNullOrWhiteSpace(lead.Phone))
                fields["Phone"] = lead.Phone;

            if (!string.IsNullOrWhiteSpace(lead.SupplierPayload))
            {
                var enrichedFields = ExtractSourceSpecificFields(lead.SupplierPayload, source);
                foreach (var kvp in enrichedFields)
                {
                    fields[kvp.Key] = kvp.Value;
                }
            }

            return fields;
        }

        /// <summary>
        /// Parse a full name into first and last name components.
        /// </summary>
        public static (string FirstName, string LastName) ParseName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("Unknown", "");

            fullName = fullName.Trim();
            var parts = fullName.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return ("Unknown", "");

            if (parts.Length == 1)
                return (parts[0], "");

            var firstName = parts[0];
            var lastName = string.Join(" ", parts, 1, parts.Length - 1);

            return (firstName, lastName);
        }

        /// <summary>
        /// Extract enrichment fields from supplier payload based on source type.
        /// </summary>
        private static Dictionary<string, object> ExtractSourceSpecificFields(string payload, LeadSource source)
        {
            var fields = new Dictionary<string, object>();

            if (string.IsNullOrWhiteSpace(payload))
                return fields;

            try
            {
                return source switch
                {
                    LeadSource.Website => ExtractWebsiteFields(payload),
                    LeadSource.Mobile => ExtractMobileFields(payload),
                    LeadSource.CRM => ExtractCRMFields(payload),
                    LeadSource.Referral => ExtractReferralFields(payload),
                    LeadSource.ThirdPartyAPI => ExtractAPIFields(payload),
                    LeadSource.EmailForm => ExtractEmailFields(payload),
                    _ => new Dictionary<string, object>()
                };
            }
            catch (Exception)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Extract enrichment from website lead (UTM parameters, referrer, etc.).
        /// </summary>
        private static Dictionary<string, object> ExtractWebsiteFields(string payload)
        {
            var fields = new Dictionary<string, object>();
            var websiteDto = JsonPayloadHelper.Deserialize<WebsiteLeadDto>(payload);

            if (websiteDto != null)
            {
                if (!string.IsNullOrWhiteSpace(websiteDto.ReferrerUrl))
                    fields["Source"] = $"Website ({websiteDto.ReferrerUrl})";

                if (!string.IsNullOrWhiteSpace(websiteDto.UtmSource))
                    fields["SourceMetadata"] = $"utm_source={websiteDto.UtmSource}";
            }

            return fields;
        }

        /// <summary>
        /// Extract enrichment from mobile lead (device info, app version, etc.).
        /// </summary>
        private static Dictionary<string, object> ExtractMobileFields(string payload)
        {
            var fields = new Dictionary<string, object>();
            var mobileDto = JsonPayloadHelper.Deserialize<MobileLeadDto>(payload);

            if (mobileDto != null)
            {
                if (!string.IsNullOrWhiteSpace(mobileDto.DeviceId))
                    fields["SourceMetadata"] = $"device={mobileDto.DeviceId}";
            }

            return fields;
        }

        /// <summary>
        /// Extract enrichment from CRM payload (first/last name separation, CRM record ID, etc.).
        /// </summary>
        private static Dictionary<string, object> ExtractCRMFields(string payload)
        {
            var fields = new Dictionary<string, object>();
            var crmDto = JsonPayloadHelper.Deserialize<CRMLeadDto>(payload);

            if (crmDto != null)
            {
                if (!string.IsNullOrWhiteSpace(crmDto.FirstName))
                    fields["FirstName"] = crmDto.FirstName;

                if (!string.IsNullOrWhiteSpace(crmDto.LastName))
                    fields["LastName"] = crmDto.LastName;
            }

            return fields;
        }

        /// <summary>
        /// Extract enrichment from referral payload.
        /// </summary>
        private static Dictionary<string, object> ExtractReferralFields(string payload)
        {
            var fields = new Dictionary<string, object>();
            var referralDto = JsonPayloadHelper.Deserialize<ReferralLeadDto>(payload);

            if (referralDto != null && referralDto.ReferrerPatientId != Guid.Empty)
            {
                fields["ReferredByPatientId"] = referralDto.ReferrerPatientId;
            }

            return fields;
        }

        /// <summary>
        /// Extract enrichment from API payload (external ID, raw payload info, etc.).
        /// </summary>
        private static Dictionary<string, object> ExtractAPIFields(string payload)
        {
            var fields = new Dictionary<string, object>();
            var apiDto = JsonPayloadHelper.Deserialize<APILeadDto>(payload);

            if (apiDto != null && !string.IsNullOrWhiteSpace(apiDto.ExternalId))
            {
                fields["ExternalId"] = apiDto.ExternalId;
            }

            return fields;
        }

        /// <summary>
        /// Extract enrichment from email form payload.
        /// </summary>
        private static Dictionary<string, object> ExtractEmailFields(string payload)
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Check if two leads likely represent the same person (deduplication).
        /// </summary>
        public static int GetDuplicateLikelihood(Lead lead1, Lead lead2)
        {
            if (lead1.TenantId != lead2.TenantId)
                return 0;

            if (!string.IsNullOrWhiteSpace(lead1.Email) && 
                !string.IsNullOrWhiteSpace(lead2.Email) &&
                lead1.Email.Equals(lead2.Email, StringComparison.OrdinalIgnoreCase))
                return 100;

            if (!string.IsNullOrWhiteSpace(lead1.Phone) &&
                !string.IsNullOrWhiteSpace(lead2.Phone) &&
                NormalizePhone(lead1.Phone) == NormalizePhone(lead2.Phone))
                return 90;

            if (!string.IsNullOrWhiteSpace(lead1.Email) &&
                !string.IsNullOrWhiteSpace(lead2.Email) &&
                lead1.Email.Contains("@") &&
                lead2.Email.Contains("@"))
            {
                var email1Domain = lead1.Email.Split('@')[1];
                var email2Domain = lead2.Email.Split('@')[1];

                if (email1Domain.Equals(email2Domain, StringComparison.OrdinalIgnoreCase) &&
                    lead1.Name.Equals(lead2.Name, StringComparison.OrdinalIgnoreCase))
                    return 75;
            }

            return 0;
        }

        /// <summary>
        /// Check if a lead and patient likely represent the same person.
        /// </summary>
        public static int GetPatientDuplicateLikelihood(Lead lead, Patient patient)
        {
            if (lead.TenantId != patient.TenantId)
                return 0;

            if (!string.IsNullOrWhiteSpace(lead.Email) &&
                !string.IsNullOrWhiteSpace(patient.Email) &&
                lead.Email.Equals(patient.Email, StringComparison.OrdinalIgnoreCase))
                return 100;

            if (!string.IsNullOrWhiteSpace(lead.Phone) &&
                !string.IsNullOrWhiteSpace(patient.Phone) &&
                NormalizePhone(lead.Phone) == NormalizePhone(patient.Phone))
                return 90;

            var leadNames = ParseName(lead.Name);
            if (leadNames.FirstName.Equals(patient.FirstName, StringComparison.OrdinalIgnoreCase) &&
                leadNames.LastName.Equals(patient.LastName, StringComparison.OrdinalIgnoreCase))
                return 75;

            return 0;
        }

        /// <summary>
        /// Normalize phone numbers for comparison (remove formatting, spaces, etc.).
        /// </summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "";

            var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
            return digits;
        }

        /// <summary>
        /// Validate that a Lead has minimum required fields for conversion to Patient.
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateLeadForConversion(Lead lead)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(lead.Name))
                errors.Add("Lead must have a name.");

            if (string.IsNullOrWhiteSpace(lead.Email) && string.IsNullOrWhiteSpace(lead.Phone))
                errors.Add("Lead must have either email or phone number.");

            if (lead.TenantId == Guid.Empty)
                errors.Add("Lead must belong to a tenant.");

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Create a Patient record from a Lead with the given enrichment fields.
        /// </summary>
        public static Patient CreatePatientFromLead(Lead lead, Dictionary<string, object> enrichedFields)
        {
            var (firstName, lastName) = ParseName(lead.Name);

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = lead.TenantId,
                FirstName = enrichedFields.ContainsKey("FirstName") 
                    ? (string)enrichedFields["FirstName"] 
                    : firstName,
                LastName = enrichedFields.ContainsKey("LastName")
                    ? (string)enrichedFields["LastName"]
                    : lastName,
                Email = enrichedFields.ContainsKey("Email")
                    ? (string)enrichedFields["Email"]
                    : lead.Email ?? "",
                Phone = lead.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            return patient;
        }
    }
}
