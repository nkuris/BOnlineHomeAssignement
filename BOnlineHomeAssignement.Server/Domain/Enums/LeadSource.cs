namespace BOnlineHomeAssignement.Server.Domain.Enums
{
    /// <summary>
    /// Identifies the source/supplier of a lead.
    /// Used to determine which field mappings and validation rules apply.
    /// </summary>
    public enum LeadSource
    {
        /// <summary>Direct online form submission from website</summary>
        Website = 1,

        /// <summary>Lead from mobile app submission</summary>
        Mobile = 2,

        /// <summary>Manual entry via admin console</summary>
        Manual = 3,

        /// <summary>Import from third-party API (generic)</summary>
        ThirdPartyAPI = 4,

        /// <summary>Import from CRM system (Salesforce, HubSpot, etc.)</summary>
        CRM = 5,

        /// <summary>Email/form submission gateway</summary>
        EmailForm = 6,

        /// <summary>Social media lead form (Facebook, LinkedIn, etc.)</summary>
        SocialMedia = 7,

        /// <summary>Phone/call center system</summary>
        CallCenter = 8,

        /// <summary>Referral from existing patient/lead</summary>
        Referral = 9,

        /// <summary>Affiliate or partner program</summary>
        Affiliate = 10,

        /// <summary>Other unclassified source</summary>
        Other = 99
    }
}
