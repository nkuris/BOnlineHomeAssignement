# ארכיטקטורת מערכת: Platform לניהול Care Programs

**גרסה:** 1.0  
**תאריך:** ספטמבר 2026  
**סטטוס:** Design Document  

---

## תוכן עניינים

1. [Executive Summary](#1-executive-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Multi-Tenant Design](#3-multi-tenant-design)
4. [Configuration & Customization](#4-configuration--customization)
5. [Workflow & Triggers](#5-workflow--triggers)
6. [Security & Privacy](#6-security--privacy)
7. [API Design](#7-api-design)
8. [Deployment & Operations](#8-deployment--operations)

---

## 1. Executive Summary

Platform זה משרת ארגונים (Tenants) בניהול תוכניות טיפול בחולים, מעקב אחר progress, ואוטומציה של תהליכים. כל tenant יכול להגדיר:
- תוכניות טיפול משלו
- שלבים וכללי transition
- טוקי עבודה מותאמים
- הסכמים וידיעות אוטומטיות

**Core Pillars:**
- ✅ Multi-tenant (נתונים מתודלקים בקפדנות)
- ✅ Configurable (אין hard-code של logic)
- ✅ Event-driven (עובד עם triggers וqueues)
- ✅ Secure (encryption, audit trails, RBAC)
- ✅ Scalable (cloud-native ready)

---

## 2. Architecture Overview

### 2.1 Diagram: High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      CLIENT LAYER (React SPA)               │
│  Dashboard | Admin Panel | Patient Portal | Reports        │
└────────────────────────┬────────────────────────────────────┘
						 │ HTTPS
						 ▼
┌─────────────────────────────────────────────────────────────┐
│              API GATEWAY & AUTHENTICATION                   │
│  • Tenant Identification (Header/Cookie)                  │
│  • JWT Validation & Role-Based Access                     │
│  • Rate Limiting & DDoS Protection                        │
└────┬──────────────┬──────────────┬──────────────┬──────────┘
	 │              │              │              │
	 ▼              ▼              ▼              ▼
┌────────────┐ ┌─────────────┐ ┌─────────────┐ ┌────────────┐
│ Patient     │ │ Program      │ │ Workflow    │ │ Admin      │
│ Service     │ │ Configuration│ │ Engine      │ │ Service    │
│             │ │             │ │             │ │            │
│ • Enroll    │ │ • Rules      │ │ • Triggers  │ │ • Users    │
│ • Profile   │ │ • Stages     │ │ • Queues   │ │ • Tenant   │
│ • History   │ │ • Forms      │ │ • Retries   │ │ • Audit    │
└────┬────────┘ └────┬────────┘ └────┬────────┘ └────┬───────┘
	 │              │               │               │
	 └──────────────┼───────────────┼───────────────┘
					│               │
					▼               ▼
		┌──────────────────────────────────┐
		│   BUSINESS LOGIC & SERVICES      │
		│  • Patient Pathway Orchestration │
		│  • Rule Evaluation Engine        │
		│  • Notification Dispatcher       │
		│  • Reporting & Analytics         │
		└───────┬───────────────────┬──────┘
				│                   │
				▼                   ▼
		┌──────────────┐  ┌──────────────────────┐
		│  DATA ACCESS │  │ EXTERNAL SERVICES    │
		│  LAYER       │  │                      │
		│              │  │ • Email (SendGrid)   │
		│ • EF Core ORM│  │ • SMS (Twilio)      │
		│ • Caching    │  │ • Calendar (iCal)   │
		│ • Query Opt. │  │ • Analytics (GA)    │
		└──────┬───────┘  └──────────┬───────────┘
			   │                     │
			   ▼                     ▼
		┌──────────────────────────────────┐
		│        SQL SERVER 2022 (Shared)  │
		│  • Patient Data                  │
		│  • Program Configurations        │
		│  • Audit Logs                    │
		│  • TenantId on every table       │
		└──────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│            BACKGROUND PROCESSING & MESSAGING               │
│  • Azure Service Bus (Queues & Topics)                     │
│  • Hangfire or Worker Service (Scheduled Jobs)             │
│  • Event Log for Event Sourcing                            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│          LOGGING, MONITORING & OBSERVABILITY               │
│  • Application Insights (Azure Monitor)                    │
│  • Structured Logging (Serilog)                            │
│  • Health Checks (/health endpoint)                        │
│  • Performance Tracing (Distributed Tracing)               │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Technology Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| **Frontend** | React + TypeScript + Vite | SPA, responsive |
| **Backend** | ASP.NET Core 10 | REST API, cloud-ready |
| **Database** | SQL Server 2022 | Relational, ACID-compliant |
| **ORM** | Entity Framework Core | Code-first migrations |
| **Caching** | Redis (optional) | Session, query cache |
| **Messaging** | Azure Service Bus / RabbitMQ | Async processing |
| **Jobs** | Hangfire / Azure Functions | Scheduled/recurring tasks |
| **Notifications** | SendGrid, Twilio, Teams | Multi-channel |
| **Logging** | Serilog + Application Insights | Structured, centralized |
| **Containerization** | Docker + Docker Compose | Dev/test/prod parity |
| **Orchestration** | Kubernetes (future) | Pod scaling, self-healing |

---

## 3. Multi-Tenant Design

### 3.1 Tenant Identification Strategy

**Method:** Header-based + Database lookup

```
Request Header:
GET /api/patients
Host: api.careplatform.com
X-Tenant-Id: tenant-abc-123  ← OR via subdomain: tenant-abc.careplatform.com
Authorization: Bearer <JWT>

Server Logic:
1. Extract TenantId from header/subdomain
2. Validate JWT claims contain same TenantId
3. Load tenant config from cache
4. Apply TenantId to all queries
```

### 3.2 Data Isolation: Database Schema

**Strategy:** Shared Database + TenantId Column (Row-Level Isolation)

```sql
-- Every table has TenantId (NOT NULL, Foreign Key)
CREATE TABLE Patients (
	Id UNIQUEIDENTIFIER PRIMARY KEY,
	TenantId UNIQUEIDENTIFIER NOT NULL,
	FirstName NVARCHAR(100) NOT NULL,
	LastName NVARCHAR(100) NOT NULL,
	Email NVARCHAR(200),
	CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
	FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
	INDEX IX_Tenant_Patient (TenantId, Id)
);

-- Query MUST filter by TenantId
SELECT * FROM Patients 
WHERE TenantId = @tenantId  ← ⚠️ MANDATORY
AND Id = @patientId;
```

**Why Shared DB?**
- ✅ Cost-efficient
- ✅ Easier maintenance (single schema)
- ✅ Simpler analytics/reporting across tenants
- ✅ Quick tenant onboarding

**Why NOT Separate DB?**
- ❌ High operational overhead
- ❌ Backup/restore complexity
- ❌ Schema versioning nightmare

### 3.3 Data Isolation Enforcement

```csharp
// Multi-Tenant DbContext Interceptor
public class TenantInterceptor : SaveChangesInterceptor
{
	private readonly ITenantProvider _tenantProvider;

	public override InterceptionResult<int> SavingChanges(/* ... */)
	{
		var tenantId = _tenantProvider.GetCurrentTenantId();

		foreach (var entity in changeTracker.Entries())
		{
			if (entity.Entity is ITenantScoped tenantScoped)
			{
				if (tenantScoped.TenantId != tenantId)
					throw new UnauthorizedAccessException(
						"Cannot modify data from different tenant");
			}
		}
		return base.SavingChanges(dbContext, eventData);
	}
}

// Applied on DbContext configuration
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
	optionsBuilder
		.AddInterceptors(new TenantInterceptor(_tenantProvider));
}
```

### 3.4 Tenant Configuration

```csharp
public class Tenant
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string? Logo { get; set; }
	public TimeZoneInfo TimeZone { get; set; }
	public bool IsActive { get; set; }

	// Configuration
	public string? BrandingColor { get; set; }
	public int MaxPatients { get; set; }
	public int MaxPrograms { get; set; }
	public List<string> EnabledFeatures { get; set; }  // List of feature flags
}
```

---

## 4. Configuration & Customization

### 4.1 Rule-Based Customization (No Hard-Code)

**Problem:** Every tenant has different rules → can't hard-code logic

**Solution:** Rule Engine + JSON Configuration

```csharp
public class BusinessRule
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid ProgramId { get; set; }

	public string Name { get; set; }           // "Auto-advance high scorers"
	public string RuleType { get; set; }       // "StageTransition", "Notification"
	public string TriggerEvent { get; set; }   // "OnFormSubmit", "OnDayX"

	// Stored as JSON
	public string Condition { get; set; }      // Decision logic
	public string Action { get; set; }         // What to do if true

	public int Priority { get; set; }          // Execution order
	public bool IsActive { get; set; }
}
```

**Example Rule (JSON):**

```json
{
  "id": "rule-123",
  "name": "Auto-advance chronic patients with high compliance",
  "triggerEvent": "OnFormSubmit",
  "condition": {
	"type": "and",
	"conditions": [
	  { "field": "patientType", "operator": "equals", "value": "Chronic" },
	  { "field": "formScore", "operator": "greaterThan", "value": 80 }
	]
  },
  "action": {
	"type": "stageTransition",
	"targetStageId": "stage-final-123",
	"notifyPatient": true,
	"message": "Congrats! You've progressed to Monitoring stage."
  },
  "priority": 1,
  "isActive": true
}
```

### 4.2 Workflow Configuration (No Code Changes)

```csharp
public class ProgramConfiguration
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid ProgramId { get; set; }

	public string WorkflowType { get; set; }   // "Linear", "Branching", "Custom"
	public string? WorkflowDefinition { get; set; }  // JSON

	// Stages defined as JSON
	public List<ProcessStage> ProcessStages { get; set; }

	// Forms customizable per stage
	public List<FormDefinition> FormDefinitions { get; set; }

	// Rules engine
	public List<BusinessRule> BusinessRules { get; set; }
}

public class ProcessStage
{
	public Guid Id { get; set; }
	public string Name { get; set; }           // "Assessment", "Treatment"
	public int Order { get; set; }
	public int? DurationDays { get; set; }     // SLA

	public string? RequiredFormIds { get; set; }      // CSV
	public string? TriggeredTaskIds { get; set; }     // CSV
	public string? AutoAdvanceConditions { get; set; } // JSON
	public string? RoleRequirements { get; set; }      // Which roles can work here
}
```

**UI for Admins:**
- Drag-and-drop workflow builder
- Visual rule engine (condition builder)
- Form designer
- No coding required

---

## 5. Workflow & Triggers

### 5.1 Event-Based vs Time-Based

| Aspect | Event-Based | Time-Based |
|--------|------------|-----------|
| **Trigger** | Form submission, stage entry | Every day at 9 AM, 3 days after enrollment |
| **Example** | "Patient submitted form → run rule" | "Check all patients needing follow-up" |
| **Latency** | Immediate (seconds) | Scheduled (minutes/hours) |
| **Use Case** | Dynamic responses | Routine tasks, reminders |
| **Implementation** | Queue (when event fires) | Cron job or Hangfire |

### 5.2 Workflow Execution Flow

```
┌─────────────────┐
│  Event Occurs   │  Form submitted / Stage entered / Rule scheduled
└────────┬────────┘
		 │
		 ▼
	┌────────────────────┐
	│ Publish to Queue   │  Azure Service Bus / RabbitMQ
	│ (PatientPathwayId) │
	└────────┬───────────┘
			 │
			 ▼
	┌────────────────────────────────┐
	│ Consumer Reads from Queue      │
	│ (Worker Service / Hangfire)    │
	└────────┬───────────────────────┘
			 │
			 ▼
	┌────────────────────────────┐
	│ 1. Find Matching Rules     │
	│ WHERE TriggerEvent = X    │
	│ AND IsActive = true        │
	└────────┬───────────────────┘
			 │
			 ▼
	┌────────────────────────────┐
	│ 2. Evaluate Conditions     │
	│ (JSON rule engine)         │
	└────────┬───────────────────┘
			 │
			 ├─ Condition FALSE → Skip rule
			 │
			 └─ Condition TRUE → Execute action
					 │
					 ▼
				┌──────────────────────┐
				│ 3. Execute Action    │
				│ • Transition stage   │
				│ • Create task        │
				│ • Send notification  │
				│ • Update data        │
				└──────────┬───────────┘
						   │
						   ▼
				┌──────────────────────┐
				│ 4. Log Execution     │
				│ • SUCCESS / FAILED   │
				│ • Timestamp          │
				│ • Error details      │
				└──────────┬───────────┘
						   │
						   ▼
				┌──────────────────────┐
				│ 5. Create Timeline   │
				│ Event (Audit Trail)  │
				└──────────────────────┘
```

### 5.3 Reliability Patterns

```csharp
public class WorkflowProcessor
{
	private readonly ISerializationProvider _serializer;
	private readonly IRetryPolicy _retryPolicy;

	public async Task ProcessWorkflowEvent(WorkflowEvent @event)
	{
		// 1. IDEMPOTENCY: Check if already processed
		if (await _executionLog.IsExecuted(@event.Id))
			return;  // Already done, skip

		int attempts = 0;
		while (attempts < 3)  // Max 3 retries
		{
			try
			{
				// 2. Load patient context
				var pathway = await _db.PatientPathways
					.Where(p => p.Id == @event.PatientPathwayId 
						   && p.TenantId == _tenantProvider.GetCurrentTenantId())
					.FirstOrDefaultAsync();

				// 3. Find and evaluate rules
				var rules = await _db.BusinessRules
					.Where(r => r.TriggerEvent == @event.EventType 
						   && r.IsActive)
					.OrderBy(r => r.Priority)
					.ToListAsync();

				foreach (var rule in rules)
				{
					// 4. Evaluate condition
					if (EvaluateCondition(rule.Condition, pathway))
					{
						// 5. Execute action (may throw)
						await ExecuteAction(rule.Action, pathway);

						// 6. Log success
						await _executionLog.MarkAsSucceeded(
							@event.Id, 
							rule.Id, 
							attempts
						);
					}
				}

				return;  // Success
			}
			catch (TransientException ex)
			{
				attempts++;
				if (attempts >= 3) throw;

				// Exponential backoff: 1s, 2s, 4s
				await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts - 1)));
			}
			catch (Exception ex)
			{
				// Not retryable, log and alert
				await _logger.LogError(ex, "Unrecoverable workflow error");
				await _alerting.SendAlert("Workflow processing failed", ex);
				throw;
			}
		}
	}
}
```

### 5.4 Scheduled Background Jobs

```csharp
// Hangfire Job Configuration
RecurringJob.AddOrUpdate<FollowUpService>(
	"daily-followup-check",
	x => x.CheckAndScheduleFollowUps(),
	Cron.Daily(9, 0)  // Every day at 9:00 AM
);

RecurringJob.AddOrUpdate<QuestionnaireService>(
	"weekly-questionnaire-send",
	x => x.SendScheduledQuestionnaires(),
	Cron.Weekly(DayOfWeek.Monday, 8, 0)  // Every Monday at 8:00 AM
);

// Implemented as
public class FollowUpService
{
	public async Task CheckAndScheduleFollowUps()
	{
		// Get all tenants (run per tenant for isolation)
		var tenants = await _db.Tenants.Where(t => t.IsActive).ToListAsync();

		foreach (var tenant in tenants)
		{
			_tenantProvider.SetCurrentTenant(tenant.Id);

			// Find all patients needing follow-up
			var pathwaysNeedingFollowUp = await _db.PatientPathways
				.Where(p => p.TenantId == tenant.Id 
					   && p.StageDueAt <= DateTime.UtcNow
					   && !p.IsCompleted)
				.ToListAsync();

			foreach (var pathway in pathwaysNeedingFollowUp)
			{
				// Send notification, create reminder task, etc.
				await ProcessFollowUp(pathway);
			}
		}
	}
}
```

---

## 6. Security & Privacy

### 6.1 Authentication & Authorization

```csharp
// JWT Token Payload (Issued by Identity Server / Azure AD)
{
  "sub": "user-id-123",           // Subject (user ID)
  "tenant": "tenant-abc-123",     // Tenant ID (hardcoded in token)
  "roles": ["Doctor", "Admin"],   // User roles within tenant
  "permissions": ["Read", "Edit", "Approve"],
  "programs": ["prog-1", "prog-2"],  // Programs user can access
  "exp": 1695998400,
  "iat": 1695912000
}

// API Endpoint Authorization
[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
	[HttpGet("{id}")]
	[Authorize(Roles = "Doctor,Nurse,CareCoordinator")]  // Role-based
	public async Task<IActionResult> GetPatient(Guid id)
	{
		// 1. Extract TenantId from JWT
		var tenantId = User.FindFirstValue("tenant");

		// 2. Verify patient belongs to tenant
		var patient = await _db.Patients
			.Where(p => p.Id == id && p.TenantId == tenantId)
			.FirstOrDefaultAsync();

		if (patient == null)
			return NotFound();  // 404 (not 403, to avoid enumeration)

		// 3. Check fine-grained permissions
		if (!await _authz.CanViewPatient(User, patient))
			return Forbid();

		// 4. Log access
		await _auditLog.LogAccess(User.Identity.Name, "PatientView", id);

		return Ok(patient);
	}
}
```

### 6.2 Data Encryption

```csharp
public class EncryptionService
{
	private readonly string _encryptionKey;  // From Key Vault

	// Encrypt Personally Identifiable Information (PII)
	public string EncryptPII(string plainText)
	{
		using (var aes = Aes.Create())
		{
			aes.Key = Convert.FromBase64String(_encryptionKey);
			aes.Mode = CipherMode.GCM;

			var cipher = aes.CreateEncryptor();
			var encryptedData = cipher.TransformFinalBlock(
				Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);

			return Convert.ToBase64String(encryptedData);
		}
	}
}

// At-rest Encryption (SQL Server TDE - Transparent Data Encryption)
// CREATE MASTER KEY;
// CREATE CERTIFICATE TdeCertificate WITH SUBJECT = 'TDE Certificate';
// CREATE DATABASE ENCRYPTION KEY WITH ALGORITHM = AES_256 
//   ENCRYPTION BY SERVER CERTIFICATE TdeCertificate;
// ALTER DATABASE YourDb SET ENCRYPTION ON;

// In-transit Encryption (HTTPS/TLS 1.3)
// All API calls use HSTS + HTTPS
```

### 6.3 Secrets Management

```csharp
// Azure Key Vault Integration
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		var keyVaultUrl = new Uri(Configuration["KeyVault:Url"]);
		var credential = new DefaultAzureCredential();

		var keyVaultClient = new SecretClient(keyVaultUrl, credential);

		services.AddScoped<ISecretProvider>(
			sp => new AzureKeyVaultSecretProvider(keyVaultClient)
		);
	}
}

// Usage: Retrieved at runtime, never committed to code
var connectionString = await _secretProvider.GetSecret("sql-connection-string");
var jwtKey = await _secretProvider.GetSecret("jwt-signing-key");
```

### 6.4 Audit Trail & Logging

```csharp
public class AuditLog
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public string UserId { get; set; }
	public string Action { get; set; }              // "Create", "Edit", "Delete"
	public string EntityType { get; set; }          // "Patient", "Program"
	public Guid? EntityId { get; set; }
	public string? OldValue { get; set; }           // Before (if edit)
	public string? NewValue { get; set; }           // After (if edit)
	public string? Reason { get; set; }
	public DateTime Timestamp { get; set; }
	public string? IpAddress { get; set; }
}

// Structured Logging (Serilog)
_logger.LogInformation(
	"Patient viewed: {@Patient} by {User} at {Timestamp}",
	patientDto,
	User.Identity.Name,
	DateTime.UtcNow
);

// Result: Searchable logs in Application Insights
//{
//  "MessageTemplate": "Patient viewed: {@Patient} by {User}...",
//  "PatientId": "patient-123",
//  "PatientName": "John Doe",
//  "User": "doctor@hospital.com",
//  "Timestamp": "2026-09-15T10:30:00Z"
//}
```

---

## 7. API Design

### 7.1 Key Endpoints

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| **GET** | `/api/patients` | List patients (paginated) | Nurse+ |
| **GET** | `/api/patients/{id}` | Get patient details | Nurse+ |
| **POST** | `/api/patients` | Create patient | Admin |
| **PUT** | `/api/patients/{id}` | Update patient | Doctor+ |
| **POST** | `/api/patients/{id}/enroll` | Enroll in program | Admin |
| **GET** | `/api/programs` | List programs | Any |
| **POST** | `/api/programs/{id}/config` | Update program rules | Admin |
| **GET** | `/api/patients/{id}/pathway` | Patient progression | Nurse+ |
| **POST** | `/api/forms/{id}/submit` | Submit questionnaire | Patient |
| **GET** | `/api/audit-logs` | Audit trail | Admin |
| **GET** | `/health` | Health check | Public |

### 7.2 API Response Format

```json
// Success (200 OK)
{
  "success": true,
  "data": {
	"id": "patient-123",
	"firstName": "John",
	"lastName": "Doe"
  },
  "meta": {
	"timestamp": "2026-09-15T10:30:00Z",
	"version": "v1"
  }
}

// Error (400 Bad Request)
{
  "success": false,
  "error": {
	"code": "VALIDATION_ERROR",
	"message": "Email is required",
	"details": {
	  "email": ["Email is required"]
	}
  },
  "requestId": "req-abc-123"  // For tracing
}
```

### 7.3 Pagination & Filtering

```
/api/patients
  ?page=1&size=20
  &sort=firstName:asc
  &filter[status]=active
  &filter[createdAfter]=2026-01-01

Response:
{
  "data": [...],
  "pagination": {
	"page": 1,
	"size": 20,
	"total": 150,
	"totalPages": 8
  }
}
```

---

## 8. Deployment & Operations

### 8.1 Environment Strategy

| Environment | Purpose | Database | Secrets | Scaling |
|-------------|---------|----------|---------|---------|
| **Dev** | Local development | LocalDB/Docker | .env file | Single instance |
| **Test** | QA & integration tests | Test DB (restored daily) | Test secrets | Manual scaling |
| **Staging** | Pre-production, load testing | Production clone | Prod-like secrets | Auto-scaling (1-3 pods) |
| **Prod** | Live system | Encrypted backups, HA | Azure Key Vault | Auto-scaling (2-10 pods) |

### 8.2 Database Migration Strategy

```csharp
// Automatic migrations on app startup (safe with downtime planning)
public class MigrationBackgroundService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Run migrations
		using (var scope = _serviceProvider.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			try
			{
				_logger.LogInformation("Applying database migrations...");
				await db.Database.MigrateAsync(stoppingToken);

				// Run seeding if needed
				var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
				await seeder.SeedIfEmptyAsync();

				_logger.LogInformation("Migrations completed successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Migration failed. Startup blocked.");
				throw;  // Prevent app from starting with stale schema
			}
		}
	}
}

// Backwards Compatibility Pattern
public class UserV2Migration : Migration
{
	protected override void Up(MigrationBuilder mb)
	{
		// Add new column as nullable
		mb.AddColumn<string>(
			name: "PhoneNumber",
			table: "Users",
			type: "nvarchar(20)",
			nullable: true  // ← Don't break existing records
		);
	}

	protected override void Down(MigrationBuilder mb)
	{
		mb.DropColumn(name: "PhoneNumber", table: "Users");
	}
}
```

### 8.3 Deployment Pipeline (CI/CD)

```
┌──────────────────┐
│ Developer Push   │  feature/auth-improvement
│ to GitHub        │
└────────┬─────────┘
		 │
		 ▼
	┌─────────────────────────────┐
	│ GitHub Actions Trigger      │
	│  • Run unit tests           │
	│  • Lint & code analysis     │
	│  • Build Docker image       │
	│  • Push to registry         │
	└────────┬────────────────────┘
			 │
			 ▼ (if tests pass)
	┌─────────────────────────────┐
	│ Deploy to Dev Environment   │
	│  • Pull image               │
	│  • Run migrations           │
	│  • Smoke tests              │
	└────────┬────────────────────┘
			 │
			 ▼ (if smoke tests pass)
	┌─────────────────────────────┐
	│ Manual Approval Required    │
	│  (Code Review + QA Sign-off)│
	└────────┬────────────────────┘
			 │
			 ▼ (if approved)
	┌─────────────────────────────┐
	│ Deploy to Staging           │
	│  • Blue-green deployment    │
	│  • Canary traffic (10%)     │
	│  • Full load tests          │
	└────────┬────────────────────┘
			 │
			 ▼ (if stable for 30 min)
	┌─────────────────────────────┐
	│ Deploy to Production        │
	│  • Gradual rollout (20-50%) │
	│  • Monitor error rates      │
	│  • Rollback button ready    │
	└─────────────────────────────┘
```

### 8.4 Monitoring & Alerting

```csharp
// Health Check Endpoint
[HttpGet("/health")]
public async Task<IActionResult> Health()
{
	var checks = new Dictionary<string, string>();

	// Database
	try
	{
		await _db.Database.ExecuteSqlAsync("SELECT 1");
		checks["database"] = "healthy";
	}
	catch { checks["database"] = "unhealthy"; }

	// Cache
	try
	{
		await _cache.GetAsync("health-check");
		checks["cache"] = "healthy";
	}
	catch { checks["cache"] = "unhealthy"; }

	// Message Queue
	try
	{
		await _messageBus.PublishAsync(new HealthCheckEvent());
		checks["messagebus"] = "healthy";
	}
	catch { checks["messagebus"] = "unhealthy"; }

	var allHealthy = checks.Values.All(v => v == "healthy");

	return allHealthy 
		? Ok(new { status = "healthy", checks })
		: StatusCode(503, new { status = "degraded", checks });
}

// Alert Thresholds (Application Insights)
[AlertRule] ErrorRateExceeds(5%)          // More than 5% errors
[AlertRule] ResponseTimeExceeds(2000ms)   // P95 latency > 2 seconds
[AlertRule] QueueDepthExceeds(1000)       // Unprocessed messages > 1000
[AlertRule] DatabaseConnectionPoolFull()  // DB connection exhaustion
```

### 8.5 Rollback Strategy

```yaml
# Deployment manifest with quick rollback
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: care-api
  annotations:
	rollback-strategy: "blue-green"
	rollback-timeout: "5m"
spec:
  replicas: 3
  strategy:
	type: "RollingUpdate"
	rollingUpdate:
	  maxSurge: 1
	  maxUnavailable: 0

  template:
	spec:
	  containers:
	  - name: api
		image: careapi:v1.2.3
		livenessProbe:
		  httpGet:
			path: /health
			port: 8080
		  initialDelaySeconds: 30
		  periodSeconds: 10

		# If health check fails, Kubernetes auto-restarts
```

**Rollback Command:**
```powershell
# If new version has critical bug
kubectl rollout undo deployment/care-api --to-revision=2

# Verify rollback
kubectl rollout status deployment/care-api
```

### 8.6 Versioning Strategy

```
API Version: v1 (backward compatible)
Database Version: 2026-09-15__001__CreatePatientTable.sql
Docker Image Tags:
  - careapi:latest
  - careapi:v1.2.3
  - careapi:v1.2.3-build.456

Breaking Changes → New API version (v2)
  GET /api/v1/patients  (old format)
  GET /api/v2/patients  (new format, more fields)

  // Support both versions for 6 months
```

---

## Summary

| Component | Decision | Rationale |
|-----------|----------|-----------|
| **Architecture** | Layered + Event-Driven | Clear separation, async processing |
| **Multi-Tenancy** | Shared DB + Row-level isolation | Cost-effective, scalable |
| **Configuration** | Rule Engine (JSON) | No code changes for customizations |
| **Workflow** | Event + Time-based triggers | Flexible, reactive system |
| **Security** | JWT + TenantId checks + Audit logs | Industry standard, compliance-ready |
| **Database** | SQL Server 2022 | ACID, performance, monitoring |
| **Deployment** | Docker + Kubernetes | Cloud-native, auto-scaling |
| **Monitoring** | Application Insights | Centralized observability |

---

**Document Status:** Ready for Review  
**Next Steps:** Detailed design for each component, API specification, testing strategy
