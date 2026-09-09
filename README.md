# BOnline Home Assignment

A full-stack ASP.NET Core + React application for managing patient leads and appointments with multi-tenant support and flexible lead sourcing from multiple channels.

## Project Overview

This application provides:
- **Lead Management**: Capture and manage leads from multiple sources with extensible supplier-specific payload handling
- **Patient Management**: Track patient information and medical history
- **Multi-Tenant Support**: Isolated data per organization with tenant-aware queries and isolation
- **API-First Architecture**: RESTful API with clear separation of concerns

## Architecture Highlights

### Technology Stack
- **Backend**: ASP.NET Core 10.0 with Entity Framework Core 10.0.12
- **Database**: SQL Server
- **Frontend**: React with Vite
- **Containerization**: Docker & Docker Compose for multi-container local development

### Key Architectural Patterns
- **Domain-Driven Design (DDD)**: Clean separation between domain models, application services, and infrastructure
- **Repository Pattern**: Abstract data access through `IRepository<T>` interface
- **Dependency Injection**: Configured in `Program.cs` with scoped service lifetimes
- **DTOs (Data Transfer Objects)**: Separate input/output contracts from domain entities
- **Factory Pattern**: Source-aware lead creation via `LeadFactory`

---

## Multi-Source Lead System

### Why Multiple Lead Sources?

Leads can originate from various channels:
- **Organic**: Website forms, mobile app, phone calls
- **Third-Party**: CRM integrations, email campaigns, affiliate networks
- **Internal**: Manual entry, referrals from existing patients
- **Scheduled/Batch**: Data imports from external systems (APIs, databases, CSV files)

Each source may provide data in different formats, contain different fields, and require different processing/enrichment logic. The multi-source lead system allows the application to:

1. **Normalize** core lead fields (Name, Email, Phone) for consistent querying
2. **Preserve** supplier-specific metadata in a flexible JSON payload
3. **Configure** per-source field mappings, validation rules, and storage strategies
4. **Extend** to new sources without modifying the core Lead entity

### Architecture

#### Lead Source Classification (`Domain/Enums/LeadSource.cs`)

```csharp
enum LeadSource
{
	Website,      // Web form submissions
	Mobile,       // Mobile app sign-ups
	Manual,       // Admin manual entry
	ThirdPartyAPI, // External webhook/API
	CRM,          // CRM platform import
	EmailForm,    // Email-based lead capture
	SocialMedia,  // Social platform inquiries
	CallCenter,   // Inbound calls
	Referral,     // Patient referrals
	Affiliate,    // Affiliate partner links
	Other         // Catch-all for unknown sources
}
```

#### Lead Entity Enhancement

The core `Lead` entity now includes:
- **`LeadSource`** (enum): Structured classification of origin
- **`SupplierPayload`** (string/JSON): Raw or enriched supplier-specific data
- **`Source`** (string): Legacy field retained for backward compatibility

```csharp
public class Lead
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	// Normalized core fields
	public string Name { get; set; }
	public string? Email { get; set; }
	public string? Phone { get; set; }

	// Source tracking & payload
	public LeadSource LeadSource { get; set; }
	public string? Source { get; set; }
	public string? SupplierPayload { get; set; } // JSON storage

	public DateTime CreatedAt { get; set; }
}
```

#### Lead Source Configuration (`Application/DTOs/Lead/LeadSourceConfig.cs`)

Each source has a registered configuration that defines:
- **DisplayName**: Human-readable name for UI/logging
- **RequiredFields**: Field validation contract
- **OptionalFields**: Fields that may vary by source
- **FieldMappings**: How supplier fields map to Lead properties
- **StoreRawPayload**: Whether to preserve complete supplier payload
- **ValidationRules**: Custom validation logic per source

Example configuration:
```csharp
LeadSourceConfigs.Get(LeadSource.Website)
{
	DisplayName = "Website Form",
	RequiredFields = ["Name", "Email"],
	OptionalFields = ["Phone", "Message"],
	FieldMappings = { "utmSource" => "Source" },
	StoreRawPayload = true
}
```

#### Supplier-Specific DTOs (`Application/DTOs/Lead/Supplier/`)

Each source type has a dedicated input DTO capturing its unique fields:

- **`WebsiteLeadDto`**: UTM parameters, referrer, device fingerprint
- **`MobileLeadDto`**: Device info, GPS coordinates, app version
- **`CRMLeadDto`**: CRM record ID, sync metadata, enriched name fields
- **`ReferralLeadDto`**: Referring patient ID, relationship type
- **`APILeadDto`**: External ID, raw JSON payload, webhook signature

#### Lead Factory (`Application/Services/LeadFactory.cs`)

Central factory for source-aware Lead creation. Provides:
- **`CreateFromWebsite()`**: Build Lead from website form submission
- **`CreateFromMobile()`**: Build Lead from mobile app
- **`CreateFromCRM()`**: Build Lead from CRM import
- **`CreateFromReferral()`**: Build Lead from patient referral
- **`CreateFromAPI()`**: Build Lead from webhook/API
- **`CreateManual()`**: Build Lead from admin entry
- **`ExtractPayload<T>()`**: Deserialize supplier payload to typed DTO

Ensures:
1. Consistent normalization of core fields
2. Correct `LeadSource` enum assignment
3. Appropriate JSON serialization of supplier-specific data
4. Field validation per source configuration

#### Lead Service (`Application/Services/LeadService.cs`)

Service layer delegates creation to `LeadFactory` and exposes:
- **`CreateLeadAsync()`**: Manual creation (backward compatible)
- **`CreateWebsiteLeadAsync()`**: Website form flow
- **`CreateMobileLeadAsync()`**: Mobile app flow
- **`CreateCRMLeadAsync()`**: CRM integration flow
- **`CreateReferralLeadAsync()`**: Referral flow
- **`CreateAPILeadAsync()`**: Webhook/API flow
- **`MapToDetailedResponseDto()`**: Include payload when needed

#### Response DTOs

- **`LeadResponseDto`**: Standard response with core fields + `LeadSource` enum + optional payload
- **`LeadDetailedResponseDto`**: Always includes `SupplierPayload` for inspection/export scenarios

#### JSON Payload Helpers (`Infrastructure/Persistence/JsonPayloadHelper.cs`)

Utility service for consistent JSON handling:
- **`Serialize<T>(obj)`**: Compact JSON serialization
- **`Deserialize<T>(json)`**: Safe deserialization with null handling
- **`MergePayloads()`**: Combine supplier data with tenant enrichment
- **`GetField<T>()`**: Extract specific field from payload
- **`HasField()`**: Check payload field existence

---

### Scheduled/Batch Lead Ingestion Pattern

The multi-source architecture supports scheduled data imports from external systems:

#### Use Cases
1. **CRM Sync Service**: Periodically fetch new leads from Salesforce/HubSpot
2. **Email List Import**: Batch import subscribers from email marketing platform
3. **Partner Data Feed**: Scheduled pull from affiliate partner API
4. **CSV/Excel Upload**: Admin imports historical lead data
5. **Webhook Aggregator**: Collect events from multiple third-party webhooks

#### Implementation Pattern

A **scheduled background service** would:

1. **Fetch** data from external source (API, database, file)
2. **Transform** supplier format → appropriate DTO (e.g., `CRMLeadDto`, `APILeadDto`)
3. **Deduplicate** against existing leads (email/phone matching)
4. **Call** appropriate `LeadService.Create*LeadAsync()` method
5. **Store** raw supplier payload for audit/debugging
6. **Log** outcome (success/failure/warning) per lead

Example pseudocode:
```csharp
public class CRMSyncService : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var newLeadsFromCRM = await _crmClient.FetchNewLeadsAsync();

			foreach (var crmRecord in newLeadsFromCRM)
			{
				var dto = MapCRMRecordToCRMLeadDto(crmRecord);
				var leadResponseDto = await _leadService.CreateCRMLeadAsync(
					tenantId: _tenantContext.Current,
					dto: dto
				);

				_logger.LogInformation(
					$"Imported lead {leadResponseDto.Id} from CRM record {crmRecord.Id}"
				);
			}

			await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
		}
	}
}
```

#### Benefits of This Architecture

- **Source Isolation**: Each channel's logic is encapsulated, not intertwined with core Lead logic
- **Extensibility**: Add new sources by creating DTO + registering config + adding factory method
- **Auditability**: Complete supplier payload preserved for compliance/debugging
- **Flexibility**: Sources can have different validation rules, field sets, and storage strategies
- **Testability**: Each source method can be unit tested independently
- **Scalability**: Scheduled services can run in separate processes/containers

---

## Project Structure

```
BOnlineHomeAssignement/
├── BOnlineHomeAssignement.Server/
│   ├── Controllers/                 # HTTP endpoints
│   ├── Domain/
│   │   ├── Entities/               # Lead, Patient, etc.
│   │   └── Enums/                  # LeadSource, etc.
│   ├── Application/
│   │   ├── DTOs/                   # Input/output contracts
│   │   │   └── Lead/
│   │   │       ├── Supplier/       # Source-specific DTOs
│   │   │       ├── LeadSourceConfig.cs
│   │   │       ├── CreateLeadDto.cs
│   │   │       ├── LeadResponseDto.cs
│   │   │       └── UpdateLeadDto.cs
│   │   └── Services/               # Business logic
│   │       ├── LeadFactory.cs      # Source-aware creation
│   │       ├── LeadService.cs      # Lead CRUD + sourcing
│   │       └── PatientService.cs
│   ├── Infrastructure/
│   │   ├── Persistence/            # Database context, migrations
│   │   ├── Repositories/           # Repository implementations
│   │   └── Middleware/
│   ├── Program.cs                  # Dependency injection setup
│   └── Migrations/                 # EF Core migrations
│
├── bonlinehomeassignement.client/   # React frontend
└── docker-compose.yml              # Local development stack
```

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose
- Node.js 18+
- Visual Studio Community 2026 (or VS Code)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repo-url>
   cd BOnlineHomeAssignement
   ```

2. **Create the `.env` file** (required for Docker)
   ```bash
   # Copy the example file and update with your settings
   cp .env.example .env
   ```

   Then edit `.env` and set a strong password for `MSSQL_SA_PASSWORD`:
   ```
   MSSQL_SA_PASSWORD=YourStrongPassword123!
   ```

   **Note**: The `.env` file is in `.gitignore` for security (it contains secrets). Each developer must create it locally after cloning.

3. **Start services with Docker Compose**
   ```bash
   docker-compose up -d
   ```
   This brings up:
   - SQL Server (port 1433)
   - ASP.NET Core API (port 5000)
   - React frontend (port 3000)

4. **Apply database migrations**
   ```bash
   cd BOnlineHomeAssignement.Server
   dotnet ef database update
   ```

5. **Run the application**
   - Backend: `dotnet run` (from `BOnlineHomeAssignement.Server/`)
   - Frontend: `npm run dev` (from `bonlinehomeassignement.client/`)

### Testing the Multi-Source API

```bash
# Website form submission
curl -X POST http://localhost:5000/api/leads/website \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 00000000-0000-0000-0000-000000000001" \
  -d '{
	"name": "John Doe",
	"email": "john@example.com",
	"phone": "555-1234",
	"utmSource": "google",
	"utmMedium": "cpc"
  }'

# Mobile app submission
curl -X POST http://localhost:5000/api/leads/mobile \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 00000000-0000-0000-0000-000000000001" \
  -d '{
	"name": "Jane Smith",
	"email": "jane@example.com",
	"deviceModel": "iPhone 15",
	"osVersion": "iOS 18"
  }'

# CRM import
curl -X POST http://localhost:5000/api/leads/crm \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 00000000-0000-0000-0000-000000000001" \
  -d '{
	"firstName": "Bob",
	"lastName": "Johnson",
	"email": "bob@example.com",
	"crmRecordId": "salesforce-12345"
  }'
```

---

## Key Changes & Rationale

### Latest: Multi-Source Lead System Support

**Change**: Added comprehensive multi-source lead ingestion architecture to support leads from diverse channels.

**Rationale**:
- **Business Need**: Leads come from website forms, mobile apps, CRM systems, email campaigns, partner feeds, and manual entry—each with different data formats and requirements.
- **Technical Solution**: Instead of forcing all sources to fit a single schema, we now:
  - Normalize core fields (Name, Email, Phone) for consistent querying
  - Store source-specific metadata in a flexible JSON `SupplierPayload` column
  - Use a configuration registry to define per-source validation and field mapping rules
  - Implement a factory pattern for consistent, type-safe lead creation
  - Support future scheduled batch imports without schema changes

**Components Added**:
- `Domain/Enums/LeadSource.cs`: Source classification enum
- `Application/DTOs/Lead/LeadSourceConfig.cs`: Per-source configuration catalog
- `Application/DTOs/Lead/Supplier/WebsiteLeadDto.cs`: Supplier-specific DTOs (website, mobile, CRM, referral, API)
- `Application/Services/LeadFactory.cs`: Source-aware factory for Lead creation
- `Infrastructure/Persistence/JsonPayloadHelper.cs`: JSON serialization utilities
- `Migrations/20260909094723_AddSupplierPayloadAndLeadSource.cs`: Schema migration for new columns

**Benefits**:
- Easy to add new lead sources without schema changes
- Source-specific validation and field enrichment
- Complete audit trail via preserved supplier payloads
- Supports future scheduled imports from external systems
- Backward compatible with existing manual lead creation

---

## Database Schema

See `/docs/ERD.mmd` for the entity-relationship diagram. Key tables:
- **Leads**: Core lead records with multi-source support
- **Patients**: Patient master records
- **Tenants**: Multi-tenant organization isolation

To render the ERD:
```bash
cd docs
mmdc -i ERD.mmd -o ERD.png
```

---

## API Endpoints

### Lead Management

- `GET /api/leads` — List leads for current tenant
- `GET /api/leads/{id}` — Get lead details
- `POST /api/leads` — Create manual lead (backward compatible)
- `POST /api/leads/website` — Create lead from website form
- `POST /api/leads/mobile` — Create lead from mobile app
- `POST /api/leads/crm` — Create lead from CRM import
- `POST /api/leads/referral` — Create lead from patient referral
- `POST /api/leads/api` — Create lead from webhook/external API
- `PUT /api/leads/{id}` — Update lead
- `DELETE /api/leads/{id}` — Delete lead

All endpoints require:
- **Authorization**: Valid JWT or tenant context
- **Tenant Isolation**: `X-Tenant-Id` header or extracted from claims

### Patient Management

- `GET /api/patients` — List patients for current tenant
- `GET /api/patients/{id}` — Get patient details
- `POST /api/patients` — Create patient
- `PUT /api/patients/{id}` — Update patient
- `DELETE /api/patients/{id}` — Delete patient

---

## Contributing

1. Follow the coding conventions established in the codebase
2. Maintain separation of concerns (Domain/Application/Infrastructure)
3. Add new lead sources via:
   - New DTO in `Application/DTOs/Lead/Supplier/`
   - Register config in `LeadSourceConfigs`
   - Add factory method to `LeadFactory`
   - Add service method to `LeadService`
   - Add controller endpoint
4. Include migrations for schema changes
5. Test thoroughly with Docker Compose

---

## Phase 3.5: Configurable Workflows & Program Management (Current)

### Vision

**Build a zero-hardcoding, customer-configurable care program system** where each healthcare organization can define their own:
- Multi-stage patient pathways
- Process statuses and SLAs  
- Dynamic forms and questionnaires
- Automated business rules
- Content templates with variable substitution
- Role-based permissions and workflows

All configuration is **stored in the database**, **multi-tenanted**, and **fully auditable**.

### Architecture

See **[WORKFLOW_ARCHITECTURE.md](WORKFLOW_ARCHITECTURE.md)** for complete documentation.

**Key Components:**
- `ProgramConfiguration` - Root of workflow definition per program
- `ProcessStage` - Sequential workflow steps with status definitions
- `PatientPathway` - Tracks patient progress through configurable workflow
- `FormDefinition` - Dynamic forms with conditional visibility
- `BusinessRule` - If-then automation rules (stage transitions, tasks, notifications)
- `ContentTemplate` - Reusable messages with variable substitution
- `RolePermission` - Granular access control by role and stage

### API Endpoints

#### Configuration Management (Admin)
```
POST   /api/programconfiguration/create
POST   /api/programconfiguration/{id}/stages        → Add process stages
POST   /api/programconfiguration/{id}/forms         → Define forms
POST   /api/programconfiguration/{id}/rules         → Create automation rules
POST   /api/programconfiguration/{id}/templates     → Create message templates
POST   /api/programconfiguration/{id}/roles         → Define role permissions
GET    /api/programconfiguration/{id}/export        → Export config as JSON
POST   /api/programconfiguration/{id}/import        → Import config
```

#### Workflow Management (Clinical)
```
POST   /api/workflow/patients/{id}/enroll                    → Start patient pathway
GET    /api/workflow/patients/{id}/pathways                  → List patient's pathways
GET    /api/workflow/pathways/{id}/required-forms            → Get current stage forms
POST   /api/workflow/pathways/{id}/submit-form               → Submit form responses
GET    /api/workflow/pathways/{id}/can-advance               → Check readiness
POST   /api/workflow/pathways/{id}/transition-next           → Auto-advance stage
POST   /api/workflow/pathways/{id}/transition-to/{stageId}   → Admin override transition
PATCH  /api/workflow/pathways/{id}/status                    → Update stage status
POST   /api/workflow/pathways/{id}/complete                  → Discharge patient
```

### Example: Chronic Care Program

**Configuration Structure:**
```json
{
  "programConfig": {
    "workflowType": "Linear",
    "patientTypes": ["Initial", "Chronic", "Complex"]
  },
  "stages": [
    {"name": "Initial Assessment", "order": 1, "durationDays": 7},
    {"name": "Treatment Planning", "order": 2, "durationDays": 14},
    {"name": "Follow-Up", "order": 3, "durationDays": 30}
  ],
  "forms": [
    {
      "title": "Health Intake",
      "applicableStages": ["Initial Assessment"],
      "fields": [
        {"name": "chronicConditions", "type": "checkbox"},
        {"name": "medications", "type": "textarea"}
      ]
    }
  ],
  "rules": [
    {
      "name": "Auto-advance on assessment complete",
      "triggerEvent": "OnFormSubmit",
      "action": {"type": "transition", "toStage": "stage-2"}
    }
  ],
  "templates": [
    {
      "contentType": "Email",
      "subject": "Your health assessment is due",
      "content": "Hi {{patientName}}, please complete {{stageName}} by {{dueDate}}"
    }
  ]
}
```

### Multi-Tenant Isolation

All entities scoped by `TenantId`:
```csharp
// Only get pathways for this tenant
var pathways = await _workflowService.GetPatientPathwaysAsync(tenantId, patientId);
// Only get configurations for this tenant
var configs = db.ProgramConfigurations.Where(c => c.TenantId == tenantId);
```

### Demo Data

Sample "Chronic Care Program" with:
- **Stages**: Initial Assessment → Treatment Planning → Follow-Up
- **Forms**: Health intake, Treatment approval
- **Rules**: Auto-advance on form submit, Overdue reminders
- **Roles**: Patient (read-only), Nurse (create/approve), Admin (full override)

### Files Added

```
Domain/Entities/WorkflowEntities.cs
  - ProgramConfiguration, ProcessStage, PatientPathway
  - FormDefinition, TaskDefinition, ContentTemplate
  - BusinessRule, RolePermission

Infrastructure/Persistence/AppDbContext.cs
  - DbSet registrations and EF Core mappings

Application/Services/WorkflowService.cs
  - Patient pathway progression orchestration

Application/Services/ConfigurationService.cs
  - Workflow configuration management

Controllers/ProgramConfigurationController.cs
  - Configuration REST API (admin endpoints)

Controllers/WorkflowController.cs
  - Workflow REST API (clinical endpoints)

Migrations/AddConfigurableWorkflowEntities.cs
  - Database schema for all workflow entities
```

### Next Steps

1. **React Admin UI** - Build workflow configuration interface
2. **Business Rules Engine** - Replace JSON with proper rule evaluation
3. **Integration Tests** - Validate patient pathway progression
4. **KPI Dashboards** - Track completion rates, time-in-stage, pathways by cohort
5. **Event Publishing** - Integrate with Phase 4 RabbitMQ for async automation

---

## Phase 4: Event-Driven Architecture (Planned - v1.1)

### Vision


### Architecture Overview
```
┌─────────────────┐
│  Web API Calls  ├────────┐
└─────────────────┘        │
                           ↓
                     ┌──────────────┐
                     │   Commands   │
                     │  (LeadConvert,│
                     │ CreateAppt)   │
                     └──────────────┘
                           │
                           ↓
                     ┌──────────────────┐
                     │   RabbitMQ       │
                     │   Message Broker │
                     └──────────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
         ↓                 ↓                 ↓
    ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐
    │Event Handler│ │ Notification│ │Email/SMS Service│
    │  (Audit)    │ │   Service   │ │   (Async)       │
    └─────────────┘ └─────────────┘ └─────────────────┘
         │                 │                 │
         └─────────────────┼─────────────────┘
                           │
                           ↓
                   ┌──────────────────┐
                   │  Side Effects    │
                   │  (DB Updates,    │
                   │   Notifications) │
                   └──────────────────┘
```

### Key Events to Publish

| Event | Trigger | Subscribers | Payload |
|-------|---------|-------------|---------|
| **LeadCreated** | Lead form submission | AI/ML scoring, CRM sync, Analytics | `{ leadId, name, email, source, timestamp }` |
| **LeadConversionRequested** | Conversion endpoint called | Email notifier, SMS notifier, Audit logger | `{ leadId, patientId, tenantId, timestamp }` |
| **PatientCreated** | Successful lead conversion | Welcome email, Onboarding, Care team notif, Auto-enrollment | `{ patientId, firstName, lastName, email, tenantId, timestamp }` |
| **PatientEnrolled** | Enrollment created | Welcome email, Care coord assign, Appointment scheduler | `{ patientId, programId, enrollmentId, tenantId, timestamp }` |
| **AppointmentScheduled** | Appointment created | Confirmation email, SMS reminder, Provider calendar, Patient notif | `{ appointmentId, patientId, scheduledStart, providerId, tenantId, timestamp }` |
| **AppointmentStatusChanged** | Status update | Notifications, Reports, Follow-up scheduler, Refund processor | `{ appointmentId, oldStatus, newStatus, reason, tenantId, timestamp }` |
| **AppointmentCompleted** | Status → "Completed" | Follow-up scheduler, Patient survey, Notes archiver, Care plan updater | `{ appointmentId, patientId, notes, providerId, tenantId, timestamp }` |
| **NotificationSent** | Email/SMS sent | Audit logger, Failed delivery handler, Compliance reporter | `{ notificationId, channel, recipientId, eventType, timestamp }` |

### Benefits

| Aspect | Current (Synchronous) | Phase 4 (Event-Driven) |
|--------|----------------------|----------------------|
| **Response Time** | Slow (waits for all ops) | Fast (fire-and-forget) |
| **Scalability** | Limited (blocking calls) | High (async processing) |
| **Decoupling** | Tight (direct calls) | Loose (message queue) |
| **Failure Tolerance** | Cascade failures | Retry & dead-letter queues |
| **Notifications** | Synchronous (delays) | Asynchronous (fast) |
| **Audit Trail** | Database-only | Message queue + DB |
| **Testing** | Integration tests needed | Unit tests + mocks |

### Implementation Steps (Phase 4 Roadmap)

#### 1. RabbitMQ Integration
```csharp
// Program.cs
services.AddRabbitMQ(options =>
{
    options.HostName = configuration["RabbitMQ:HostName"];
    options.Port = int.Parse(configuration["RabbitMQ:Port"]);
    options.UserName = configuration["RabbitMQ:UserName"];
    options.Password = configuration["RabbitMQ:Password"];
});

services.AddEventPublisher<IEventPublisher, RabbitMqEventPublisher>();
services.AddEventHandlers(typeof(Program).Assembly);
```

#### 2. Define Event Classes
```csharp
// Core/Events/DomainEvent.cs
public abstract class DomainEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; }
}

// Events/PatientCreatedEvent.cs
public class PatientCreatedEvent : DomainEvent
{
    public Guid PatientId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public Guid TenantId { get; set; }
}
```

#### 3. Create Event Handlers (Subscribers)
```csharp
// EventHandlers/PatientCreatedEventHandler.cs
public class PatientCreatedEventHandler : IEventHandler<PatientCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PatientCreatedEventHandler> _logger;

    public async Task HandleAsync(PatientCreatedEvent @event)
    {
        try
        {
            await _emailService.SendWelcomeEmailAsync(@event.Email, @event.FirstName);
            _logger.LogInformation($"Welcome email sent for patient {@event.PatientId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email");
            throw; // RabbitMQ will retry
        }
    }
}
```

#### 4. Publish Events from Services
```csharp
// LeadConversionService.cs - Modified
public async Task<ConversionResultDto> ConvertLeadToPatientAsync(Guid tenantId, Guid leadId)
{
    // ... existing conversion logic ...
    var patient = await _patientRepository.AddAsync(patientEntity);
    await _patientRepository.SaveChangesAsync();

    // Publish event
    await _eventPublisher.PublishAsync(new PatientCreatedEvent
    {
        PatientId = patient.Id,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        Email = patient.Email,
        TenantId = tenantId
    });

    return ConversionResultDto.Success(patient.Id, enrollmentId);
}
```

#### 5. Docker Compose Configuration
```yaml
# docker-compose.yml
services:
  rabbitmq:
    image: rabbitmq:3.13-management
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # Management UI (guest/guest)
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
      RABBITMQ_DEFAULT_VHOST: /
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 10s
      retries: 5
```

#### 6. Configuration (appsettings.json)
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "bonline.events",
    "RetryPolicy": {
      "MaxRetries": 3,
      "InitialDelayMs": 1000,
      "BackoffMultiplier": 2
    }
  },
  "Email": {
    "Provider": "SendGrid",
    "SendGridApiKey": "${SENDGRID_API_KEY}",
    "FromAddress": "noreply@bonline.health"
  },
  "Sms": {
    "Provider": "Twilio",
    "TwilioAccountSid": "${TWILIO_ACCOUNT_SID}",
    "TwilioAuthToken": "${TWILIO_AUTH_TOKEN}",
    "FromNumber": "+1234567890"
  }
}
```

### Migration Path from Phase 3 to Phase 4

1. Install RabbitMQ and update `docker-compose.yml`
2. Define domain events inheriting from `DomainEvent`
3. Create event handlers implementing `IEventHandler<T>`
4. Inject `IEventPublisher` into services
5. Call `PublishAsync()` after state changes
6. Register handlers in `Program.cs` via `AddEventHandlers()`
7. Update configuration with RabbitMQ settings
8. Test end-to-end event flow
9. Monitor queues via RabbitMQ Management UI
10. Implement dead-letter queue handling for failed messages

### Future Opportunities (Phase 4+)
- **Real-time UI updates** via SignalR integration
- **Advanced scheduling** with ML-powered availability optimization
- **Patient portal** for self-service management
- **Video conferencing** for virtual appointments (Agora, Vonage)
- **SMS/Push notifications** via Twilio, Firebase
- **Analytics dashboard** with real-time metrics
- **AI appointment reminders** and no-show prevention
- **Multi-language support** (i18n)
- **Mobile app** (React Native or Flutter)

---

## Documentation

- `/docs/README.md` — ERD and diagram instructions
- This README — Architecture and getting started guide
- `IMPLEMENTATION_SUMMARY.md` — Feature-by-feature implementation details

---

## License

[Your License Here]

## Contact

For questions, create an issue or contact the development team.
