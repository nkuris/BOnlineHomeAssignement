# Multi-Source Lead System Implementation Summary

## Overview
Successfully implemented a comprehensive multi-source lead ingestion architecture supporting leads from diverse channels (website, mobile, CRM, API webhooks, patient referrals) with flexible supplier-specific payload handling and extensible configuration.

---

## What Changed & Why

### The Problem
Originally, the Lead system only had a simple `Source` string field with no structured support for:
- Different lead origins (website, mobile, CRM, email, etc.)
- Source-specific fields and metadata
- Flexible validation rules per supplier
- Audit trails of raw supplier data
- Future extensibility to new lead sources

### The Solution
Implemented a **hybrid normalized + JSON payload architecture**:
1. **Normalized Core Fields**: `Name`, `Email`, `Phone` remain queryable and consistent
2. **Structured Source Classification**: New `LeadSource` enum for known suppliers
3. **Flexible Payload Storage**: New `SupplierPayload` JSON column for source-specific data
4. **Configuration Registry**: `LeadSourceConfigs` defines per-source validation and field mappings
5. **Factory Pattern**: `LeadFactory` provides type-safe, source-aware lead creation
6. **Batch Import Support**: Architecture ready for scheduled data imports from external systems

---

## Implementation Details

### New Files Created

#### Domain Layer
- **`Domain/Enums/LeadSource.cs`**
  - Enum with 11 source types: Website, Mobile, Manual, ThirdPartyAPI, CRM, EmailForm, SocialMedia, CallCenter, Referral, Affiliate, Other
  - Provides structured classification instead of free-text strings

#### Application Layer - DTOs
- **`Application/DTOs/Lead/LeadSourceConfig.cs`**
  - Centralized configuration catalog for each lead source
  - Defines: DisplayName, RequiredFields, OptionalFields, FieldMappings, StoreRawPayload, ValidationRules
  - Static registry pattern (`LeadSourceConfigs.Get()`, `.GetAll()`)

- **`Application/DTOs/Lead/Supplier/WebsiteLeadDto.cs`** (single file with multiple DTOs)
  - `WebsiteLeadDto`: UTM parameters, referrer, device fingerprint
  - `MobileLeadDto`: Device model, OS version, app version, GPS
  - `CRMLeadDto`: CRM record ID, first/last name separation, sync metadata
  - `ReferralLeadDto`: Referring patient ID, relationship type
  - `APILeadDto`: External ID, raw JSON payload, webhook signature

- **`Application/DTOs/Lead/LeadResponseDto.cs`** (enhanced)
  - Added: `LeadSource` enum property
  - Added: Optional `SupplierPayload` field (only included on detailed responses)
  - New: `LeadDetailedResponseDto` subclass for payload inspection scenarios
  - Backward compatible: Keeps legacy `Source` string field

#### Application Layer - Services
- **`Application/Services/LeadFactory.cs`**
  - Factory methods for each source type:
	- `CreateFromWebsite(tenantId, WebsiteLeadDto)`
	- `CreateFromMobile(tenantId, MobileLeadDto)`
	- `CreateFromCRM(tenantId, CRMLeadDto)`
	- `CreateFromReferral(tenantId, ReferralLeadDto)`
	- `CreateFromAPI(tenantId, APILeadDto)`
	- `CreateManual(tenantId, CreateLeadDto)` (backward compatible)
  - Generic: `ExtractPayload<T>(payload)` for typed deserialization
  - Ensures consistent normalization and JSON serialization per source

- **`Application/Services/LeadService.cs`** (refactored)
  - Delegates creation to `LeadFactory`
  - New methods for each source:
	- `CreateWebsiteLeadAsync(tenantId, WebsiteLeadDto)`
	- `CreateMobileLeadAsync(tenantId, MobileLeadDto)`
	- `CreateCRMLeadAsync(tenantId, CRMLeadDto)`
	- `CreateReferralLeadAsync(tenantId, ReferralLeadDto)`
	- `CreateAPILeadAsync(tenantId, APILeadDto)`
  - Preserved: `CreateLeadAsync(tenantId, CreateLeadDto)` for manual entry
  - New: `MapToDetailedResponseDto()` for payload inclusion
  - Existing CRUD operations remain unchanged

#### Infrastructure Layer
- **`Infrastructure/Persistence/JsonPayloadHelper.cs`**
  - Utility class for consistent JSON operations:
	- `Serialize<T>(obj)` / `SerializePretty<T>(obj)`
	- `Deserialize<T>(json)` with null-safety
	- `MergePayloads(payload1, payload2)` for combining supplier + tenant data
	- `GetField<T>(json, fieldName)` for extracting specific fields
	- `HasField(json, fieldName)` for checking field existence
  - Centralized configuration with standard naming policies (camelCase)

#### Domain Layer - Entity
- **`Domain/Entities/Lead.cs`** (enhanced)
  - Added: `LeadSource LeadSource` (enum, default: Manual)
  - Added: `string? SupplierPayload` (JSON storage)
  - Preserved: `string? Source` (backward compatibility)
  - All other fields unchanged

#### Migrations
- **`Migrations/20260909094723_AddSupplierPayloadAndLeadSource.cs`**
  - Adds `LeadSource` column (int, maps to enum, default: 0 = Manual)
  - Adds `SupplierPayload` column (nvarchar(max), nullable)
  - Reversible (Down migration removes both columns)

### Updated Files
- **`Application/DTOs/Lead/LeadResponseDto.cs`**: Enhanced to include `LeadSource` and optional `SupplierPayload`
- **`Application/Services/LeadService.cs`**: Refactored to use `LeadFactory` and support multi-source creation

---

## Usage Patterns

### Direct API Lead Creation (Website Form)
```csharp
var dto = new WebsiteLeadDto 
{ 
	Name = "John Doe",
	Email = "john@example.com",
	UTMSource = "google",
	UTMCampaign = "promo_2024"
};

var leadResponse = await _leadService.CreateWebsiteLeadAsync(tenantId, dto);
// Lead.LeadSource = Website
// Lead.SupplierPayload = {"utmSource":"google","utmCampaign":"promo_2024",...}
```

### Scheduled CRM Import (Background Service)
```csharp
var crmRecords = await _crmClient.FetchNewLeadsAsync();
foreach (var record in crmRecords)
{
	var dto = new CRMLeadDto 
	{ 
		FirstName = record.FirstName,
		LastName = record.LastName,
		Email = record.Email,
		CrmRecordId = record.Id
	};

	var leadResponse = await _leadService.CreateCRMLeadAsync(tenantId, dto);
	// Lead.LeadSource = CRM
	// Lead.SupplierPayload = {"firstName":"...","crmRecordId":"...",...}
}
```

### Extracting Supplier Payload (For Audit/Migration)
```csharp
var lead = await _leadService.GetLeadAsync(tenantId, leadId);
var detailedLead = await _leadService.GetLeadDetailedAsync(tenantId, leadId);

// detailedLead.SupplierPayload contains full original data
var originalCRMData = JsonPayloadHelper.Deserialize<CRMLeadDto>(detailedLead.SupplierPayload);
```

---

## Architecture Benefits

### 1. **Extensibility**
- Add new lead source by:
  - Creating new DTO class in `Supplier/` folder
  - Registering config in `LeadSourceConfigs`
  - Adding factory method to `LeadFactory`
  - Adding service method to `LeadService`
  - Adding controller endpoint
- No schema changes required for core Lead table

### 2. **Flexibility**
- Each source can have completely different fields
- Per-source validation rules via `LeadSourceConfigs`
- Per-source field mapping transformations
- Optional per-source special processing

### 3. **Auditability**
- Complete raw supplier payload preserved in `SupplierPayload`
- Can reconstruct original data for compliance/debugging
- Useful for troubleshooting data quality issues

### 4. **Queryability**
- Normalized core fields (Name, Email, Phone) are indexed and queryable
- Fast "find by email" queries unaffected by payload variations
- `LeadSource` enum enables filtering by channel

### 5. **Backward Compatibility**
- Legacy `Source` string field retained
- `CreateLeadAsync(tenantId, CreateLeadDto)` still available
- Existing integrations continue working

### 6. **Scalability**
- Supports batch imports via scheduled services
- Factory pattern enables parallel processing
- JSON payload scales to any supplier complexity
- Multi-tenant isolation maintained

---

## Future Enhancement: Scheduled Batch Imports

The architecture supports adding background services for data imports:

### CRM Sync Service (Hourly)
```csharp
public class CRMSyncService : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var tenants = await _tenantService.GetAllAsync();
			foreach (var tenant in tenants)
			{
				var newLeads = await _crmClient.FetchNewLeadsAsync(tenant.CrmApiKey);
				foreach (var crmLead in newLeads)
				{
					var dto = MapCRMRecordToDto(crmLead);
					await _leadService.CreateCRMLeadAsync(tenant.Id, dto);
				}
			}
			await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
		}
	}
}
```

### Email List Import Service
- Scheduled daily/weekly pull from email marketing platform
- Deduplication against existing leads (email matching)
- Bulk insert optimized for large batches

### Affiliate Partner API Service
- Poll partner APIs for new referral leads
- Map partner-specific format to ReferralLeadDto
- Track sync state per partner

---

## Build & Compilation Status

✅ **Build Successful** - All changes compile without errors
- No breaking changes to existing code
- LeadFactory and LeadService methods added (additive changes)
- Migration generated and ready to apply

---

## Next Steps

### Phase 2 - Controller Integration (Recommended)
1. Update `LeadsController.cs` to expose new source-specific endpoints
2. Add POST endpoints:
   - `POST /api/leads/website`
   - `POST /api/leads/mobile`
   - `POST /api/leads/crm`
   - `POST /api/leads/referral`
   - `POST /api/leads/api`
3. Add GET endpoint option for detailed lead including payload:
   - `GET /api/leads/{id}?includePayload=true`

### Phase 3 - Database & Deployment
1. Apply migration to development database: `dotnet ef database update`
2. Test multi-source lead creation via API
3. Validate Docker Compose stack
4. Prepare migration scripts for production

### Phase 4 - Testing & Documentation
1. Add unit tests for LeadFactory source methods
2. Add integration tests for each source DTO validation
3. Add E2E tests for controller endpoints
4. Document API specification for consumer integrations

---

## Documentation

See **`README.md`** for:
- Architecture overview and diagrams
- Detailed explanation of each component
- Scheduled/batch import pattern documentation
- Getting started guide
- API endpoint specifications
- Contributing guidelines

---

## Files Summary

| File | Purpose | Status |
|------|---------|--------|
| Domain/Enums/LeadSource.cs | Source classification | ✅ Created |
| Domain/Entities/Lead.cs | Core entity | ✅ Enhanced |
| Application/DTOs/Lead/LeadSourceConfig.cs | Config registry | ✅ Created |
| Application/DTOs/Lead/Supplier/*.cs | Source-specific DTOs | ✅ Created |
| Application/DTOs/Lead/LeadResponseDto.cs | Response contracts | ✅ Enhanced |
| Application/Services/LeadFactory.cs | Factory pattern | ✅ Created |
| Application/Services/LeadService.cs | Service layer | ✅ Refactored |
| Infrastructure/Persistence/JsonPayloadHelper.cs | JSON utilities | ✅ Created |
| Migrations/20260909094723_*.cs | Schema migration | ✅ Generated |
| README.md | Comprehensive documentation | ✅ Created |

---

## Questions Answered

**Q: Do we need to add explanation about other kinds of lead sources?**
Yes! The README includes a dedicated section "Scheduled/Batch Lead Ingestion Pattern" explaining:
- How background services can fetch from external systems (CRM, email platforms, APIs, databases, CSV files)
- Implementation pattern with pseudocode
- Benefits of this architecture for future extensibility
- Example use cases for automated imports

This addresses the concern about supporting leads consumed by scheduled services from external data sources.

---

**Created by**: AI Assistant
**Date**: 2024
**Status**: Complete - Ready for Phase 2 (Controller Integration)
