using System.Collections.Generic;
using BOnlineHomeAssignement.Server.Domain.Enums;

namespace BOnlineHomeAssignement.Server.Application.DTOs.Lead
{
    /// <summary>
    /// Defines configuration for each lead source/supplier.
    /// Maps supplier-specific fields to standard Lead properties and validation rules.
    /// </summary>
    public class LeadSourceConfig
    {
        /// <summary>Source identification</summary>
        public LeadSource Source { get; set; }

        /// <summary>Display name for the source</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Required fields that must be present in supplier payload</summary>
        public List<string> RequiredFields { get; set; } = new();

        /// <summary>Optional fields that can be captured from supplier</summary>
        public List<string> OptionalFields { get; set; } = new();

        /// <summary>
        /// Field mappings: maps supplier field names to Lead properties
        /// Example: { "contactName" -> "Name", "phone_number" -> "Phone" }
        /// </summary>
        public Dictionary<string, string> FieldMappings { get; set; } = new();

        /// <summary>Whether to store the raw supplier payload in SupplierPayload column</summary>
        public bool StoreRawPayload { get; set; } = true;

        /// <summary>Custom validation rules for this source</summary>
        public List<string> ValidationRules { get; set; } = new();
    }

    /// <summary>
    /// Static configurations for all known lead sources.
    /// Centralizes supplier field definitions and mapping logic.
    /// </summary>
    public static class LeadSourceConfigs
    {
        private static readonly Dictionary<LeadSource, LeadSourceConfig> Configs
            = new()
            {
                {
                    LeadSource.Website,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.Website,
                        DisplayName = "Website Form",
                        RequiredFields = new() { "name", "email" },
                        OptionalFields = new() { "phone", "message", "utmSource", "utmMedium", "utmCampaign" },
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "email", "Email" },
                            { "phone", "Phone" },
                            { "phone_number", "Phone" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new() { "EmailFormatIsValid", "NameNotEmpty" }
                    }
                },
                {
                    LeadSource.Mobile,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.Mobile,
                        DisplayName = "Mobile App",
                        RequiredFields = new() { "name", "email" },
                        OptionalFields = new() { "phone", "deviceId", "appVersion", "location" },
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "email", "Email" },
                            { "phone", "Phone" },
                            { "device_id", "DeviceId" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new() { "EmailFormatIsValid" }
                    }
                },
                {
                    LeadSource.CRM,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.CRM,
                        DisplayName = "CRM System",
                        RequiredFields = new() { "firstName", "lastName" },
                        OptionalFields = new() { "email", "phone", "company", "title", "externalId" },
                        FieldMappings = new()
                        {
                            { "firstName", "FirstName" },
                            { "lastName", "LastName" },
                            { "email", "Email" },
                            { "phone", "Phone" },
                            { "full_name", "Name" },
                            { "phone_number", "Phone" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new() { "FirstNameNotEmpty", "LastNameNotEmpty" }
                    }
                },
                {
                    LeadSource.Manual,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.Manual,
                        DisplayName = "Manual Entry",
                        RequiredFields = new() { "name" },
                        OptionalFields = new() { "email", "phone", "notes" },
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "email", "Email" },
                            { "phone", "Phone" }
                        },
                        StoreRawPayload = false,
                        ValidationRules = new()
                    }
                },
                {
                    LeadSource.EmailForm,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.EmailForm,
                        DisplayName = "Email Form Gateway",
                        RequiredFields = new() { "name", "email" },
                        OptionalFields = new() { "phone", "subject", "message", "emailFromAddress" },
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "senderEmail", "Email" },
                            { "email", "Email" },
                            { "phone", "Phone" },
                            { "phone_number", "Phone" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new() { "EmailFormatIsValid" }
                    }
                },
                {
                    LeadSource.SocialMedia,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.SocialMedia,
                        DisplayName = "Social Media Lead Form",
                        RequiredFields = new() { "name" },
                        OptionalFields = new() { "email", "phone", "socialMediaId", "platform" },
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "email", "Email" },
                            { "phone", "Phone" },
                            { "contact_email", "Email" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new()
                    }
                },
                {
                    LeadSource.Referral,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.Referral,
                        DisplayName = "Referral",
                        RequiredFields = new() { "name", "referrerPatientId" },
                        OptionalFields = new() { "email", "phone", "referralNotes" },
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "email", "Email" },
                            { "phone", "Phone" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new() { "ReferrerPatientExists" }
                    }
                },
                {
                    LeadSource.Other,
                    new LeadSourceConfig
                    {
                        Source = LeadSource.Other,
                        DisplayName = "Other Source",
                        RequiredFields = new() { "name" },
                        OptionalFields = new(),
                        FieldMappings = new()
                        {
                            { "name", "Name" },
                            { "email", "Email" },
                            { "phone", "Phone" }
                        },
                        StoreRawPayload = true,
                        ValidationRules = new()
                    }
                }
            };

        /// <summary>Get configuration for a specific lead source</summary>
        public static LeadSourceConfig? Get(LeadSource source)
        {
            return Configs.TryGetValue(source, out var config) ? config : null;
        }

        /// <summary>Get all configured sources</summary>
        public static IEnumerable<LeadSourceConfig> GetAll()
        {
            return Configs.Values;
        }

        /// <summary>Check if a source has a specific field mapping</summary>
        public static bool TryGetFieldMapping(LeadSource source, string supplierFieldName, out string? leadPropertyName)
        {
            leadPropertyName = null;
            if (Configs.TryGetValue(source, out var config))
            {
                return config.FieldMappings.TryGetValue(supplierFieldName, out leadPropertyName);
            }
            return false;
        }
    }
}
