# System Architecture: Care Programs Platform
## Executive Summary (1-2 Pages)

---

## 1. System Overview

**This platform** serves organizations (Tenants) in managing patient care programs with workflows, stages, rules, and automation.

**Core Components:**
```
┌─────────────────────────────────────────────────────┐
│  Frontend (React SPA)                               │
│  Dashboard | Admin | Patient Portal                 │
└──────────────────┬──────────────────────────────────┘
				   │ HTTPS + JWT
				   ▼
┌─────────────────────────────────────────────────────┐
│  API Gateway & Auth                                 │
│  • TenantId validation                              │
│  • JWT token verification                           │
│  • Role-based access control                        │
└──────────────────┬──────────────────────────────────┘
				   │
		┌──────────┼──────────┐
		▼          ▼          ▼
	┌────────┐ ┌────────┐ ┌────────┐
	│Patient │ │Program │ │Workflow│
	│Service │ │Service │ │Engine  │
	└────┬───┘ └────┬───┘ └────┬───┘
		 │          │          │
		 └──────────┼──────────┘
					▼
		 ┌──────────────────────┐
		 │  SQL Server 2022     │
		 │  (Shared DB + ROWLS) │
		 └──────────────────────┘
					│
		┌───────────┼───────────┐
		▼           ▼           ▼
   ┌─────────┐ ┌──────────┐ ┌────────────┐
   │Patients │ │Programs  │ │AuditLogs   │
   │Enrollments│ │Rules     │ │Events      │
   │Pathways │ │Stages    │ │Triggers    │
   └─────────┘ └──────────┘ └────────────┘
```

---

## 2. Multi-Tenant Architecture

### Data Isolation Strategy
```sql
-- SHARED DATABASE + Row-Level Security (RLS)
CREATE TABLE Patients (
	Id UNIQUEIDENTIFIER PRIMARY KEY,
	TenantId UNIQUEIDENTIFIER NOT NULL,  -- ← Filter key
	FirstName NVARCHAR(100) NOT NULL,
	LastName NVARCHAR(100) NOT NULL,
	FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
	INDEX IX_Tenant (TenantId, Id)
);

-- EVERY query must filter by TenantId
SELECT * FROM Patients WHERE TenantId = @tenantId;
```

### Tenant Identification
- **Header-based:** `X-Tenant-Id: tenant-abc-123`
- **JWT Token:** TenantId hardcoded in token claims
- **Subdomain:** `tenant-abc.careplatform.com`

**Enforcement:**
```csharp
// DbContext interceptor prevents cross-tenant queries
public class TenantInterceptor : SaveChangesInterceptor
{
	public override void SavingChanges(/* ... */)
	{
		if (entity.TenantId != _currentTenantId)
			throw new UnauthorizedAccessException();
	}
}
```

---

## 3. Customization Without Hard-Code

### BusinessRule Engine (JSON-Based)
```json
{
  "name": "Auto-advance high scorers",
  "triggerEvent": "OnFormSubmit",
  "condition": {
	"type": "and",
	"conditions": [
	  { "field": "patientType", "op": "eq", "value": "Chronic" },
	  { "field": "formScore", "op": "gt", "value": 80 }
	]
  },
  "action": {
	"type": "stageTransition",
	"targetStageId": "stage-123",
	"notifyPatient": true
  }
}
```

**How it works:**
1. Admin defines rules via UI (no coding)
2. Rules stored as JSON in database
3. Rule Engine evaluates conditions at runtime
4. Actions executed automatically (transitions, notifications, tasks)

---

## 4. Workflow Execution & Triggers

### Event-Based Processing
```
Form Submitted
	↓
[Publish to Queue] (Azure Service Bus / RabbitMQ)
	↓
[Worker reads from queue]
	↓
[Find matching rules where TriggerEvent = "OnFormSubmit"]
	↓
[Evaluate conditions using JSON rule engine]
	↓
[If TRUE: Execute action (transition, notify, create task)]
	↓
[Log execution + Create timeline event]
	↓
[Send to different queues if additional actions needed]
```

### Time-Based Jobs (Hangfire)
```csharp
// Runs every day at 9 AM
RecurringJob.AddOrUpdate<FollowUpService>(
	"daily-followups",
	x => x.CheckAndScheduleFollowUps(),
	Cron.Daily(9, 0)
);

// Check all patients needing follow-up, send reminders
```

### Reliability
- ✅ **Idempotency:** Track executed events, don't reprocess
- ✅ **Retries:** Exponential backoff (1s, 2s, 4s) for transient failures
- ✅ **Dead-letter queues:** Failed messages for manual review
- ✅ **Audit trail:** Every trigger execution logged

---

## 5. Security & Compliance

| Layer | Implementation |
|-------|-----------------|
| **Authentication** | JWT tokens (Azure AD / Identity Server) |
| **Authorization** | RBAC (Role-Based Access Control) per tenant + program |
| **Encryption** | TLS 1.3 in-transit, AES-256 at-rest (SQL TDE) |
| **Secrets** | Azure Key Vault (DB credentials, API keys) |
| **Audit** | Complete audit trail (who, what, when, why) |
| **Data Isolation** | TenantId + DbContext interceptors |

**Endpoint Protection:**
```csharp
[Authorize(Roles = "Doctor,Nurse")]
public IActionResult GetPatient(Guid patientId)
{
	// 1. Verify TenantId matches JWT
	// 2. Query filters by TenantId
	// 3. Check role permissions
	// 4. Log access to audit trail
	// 5. Return 404 if not found (avoid enumeration)
}
```

---

## 6. API Design & Key Endpoints

| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/api/patients` | GET | List patients (paginated) | Nurse+ |
| `/api/patients/{id}` | GET | Get patient details | Nurse+ |
| `/api/patients/{id}/enroll` | POST | Enroll in program | Admin |
| `/api/programs/{id}/config` | PUT | Update program rules | Admin |
| `/api/patients/{id}/pathway` | GET | View patient progress | Nurse+ |
| `/api/forms/{id}/submit` | POST | Submit questionnaire | Patient |
| `/api/audit-logs` | GET | View audit trail | Admin |
| `/health` | GET | Health check | Public |

**Response Format:**
```json
{
  "success": true,
  "data": { "id": "patient-123", "name": "John Doe" },
  "meta": { "timestamp": "2026-09-15T10:30:00Z", "requestId": "req-abc" }
}
```

---

## 7. Deployment & Operations

### Environments
| Env | Database | Auto-scaling | Backups | Monitoring |
|-----|----------|--------------|---------|------------|
| **Dev** | LocalDB | No | Manual | Minimal |
| **Test** | Test (restored daily) | No | Daily | Detailed |
| **Staging** | Prod clone | Yes (1-3 pods) | Hourly | Full |
| **Prod** | HA + encrypted backups | Yes (2-10 pods) | Real-time | 24/7 |

### CI/CD Pipeline
```
GitHub Push
	↓
[Run tests + build Docker image]
	↓ (if pass)
[Deploy to Dev]
	↓
[Run smoke tests]
	↓ (manual approval)
[Deploy to Staging with canary (10%)]
	↓ (if stable 30 min)
[Gradual rollout to Prod (20% → 50% → 100%)]
	↓
[Monitor error rates + rollback button ready]
```

### Monitoring & Alerts
- **Health Check:** `/health` returns DB/Cache/Queue status
- **Alerts:** Error rate > 5%, Latency P95 > 2s, Queue depth > 1000
- **Logs:** Structured logging (Serilog) → Application Insights
- **Rollback:** `kubectl rollout undo deployment/care-api`

---

## 8. Technology Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | React + TypeScript + Vite |
| **Backend API** | ASP.NET Core 10 |
| **Database** | SQL Server 2022 |
| **ORM** | Entity Framework Core |
| **Messaging** | Azure Service Bus / RabbitMQ |
| **Jobs** | Hangfire |
| **Caching** | Redis (optional) |
| **Notifications** | SendGrid (Email), Twilio (SMS) |
| **Logging** | Serilog + Application Insights |
| **Containers** | Docker + Docker Compose |
| **Orchestration** | Kubernetes (future) |

---

## Summary Table

| Aspect | Decision | Why |
|--------|----------|-----|
| **Multi-Tenancy** | Shared DB + Row filtering | Scalable, cost-effective |
| **Customization** | JSON rules engine | No code changes needed |
| **Workflow** | Event + time-based | Flexible, reactive |
| **Reliability** | Queue + retries | Handles failures gracefully |
| **Security** | JWT + TenantId checks | Industry standard |
| **Scaling** | Kubernetes + auto-scaling | Cloud-native ready |
| **Data Isolation** | DbContext interceptors | Enforced at ORM level |

---

**Ready for Development | Next: Detailed API spec + Data model**
