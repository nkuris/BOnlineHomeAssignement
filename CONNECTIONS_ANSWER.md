# Summary: Connection Between Workflow and Other Entities ✅

## Your Question Answered

> **"What is the connection between workflow and other entities?"**

The workflow system connects to the existing domain model through a **configuration-based architecture** that creates a bridge between **business processes** (what should happen) and **patient data** (who is doing it).

---

## Connection Architecture

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      LEAD SOURCES                               │
│               (Supplier, Referral, Portal)                      │
└──────────────────────────┬──────────────────────────────────────┘
						   │
						   ▼
					┌──────────────┐
					│ LEAD ENTITY  │
					│ - Email      │
					│ - Phone      │
					│ - Source     │
					└──────┬───────┘
						   │ (Conversion)
						   ▼
					┌──────────────┐
					│ PATIENT      │
					│ - Name       │
					│ - Contact    │
					│ - Status     │
					└──────┬───────┘
						   │ (Auto-Enrollment)
						   ▼
					┌──────────────┐
					│ ENROLLMENT   │
					│ - PatientId  │
					│ - ProgramId  │
					└──────┬───────┘
						   │
				┌──────────┴──────────┐
				│                     │
		  (Optional Link)        (Required)
				│                     │
				▼                     ▼
		  ┌──────────────┐   ┌─────────────────────┐
		  │ APPOINTMENT  │   │ PATIENT PATHWAY     │
		  │              │   │ (Runtime State)     │
		  │ (Scheduling) │   │ - Current Stage     │
		  └──────────────┘   │ - Form Responses    │
							 │ - Status            │
							 └────────┬────────────┘
									  │
							 ┌────────▼──────────────┐
							 │ PROGRAM CONFIGURATION │
							 │      (Setup)          │
							 │                       │
							 ├─ ProcessStage        │
							 ├─ FormDefinition      │
							 ├─ TaskDefinition      │
							 ├─ BusinessRule        │
							 ├─ ContentTemplate     │
							 └─ RolePermission      │
```

---

## Detailed Connections

### 1️⃣ Lead → Patient → Workflow Enrollment

```csharp
// Step 1: Lead arrives from external source
Lead
  ├─ TenantId: "customer-A"
  ├─ Email: "john@example.com"
  └─ SupplierPayload: { ...external fields... }

// Step 2: Lead converts to Patient
Patient
  ├─ TenantId: "customer-A"  // Same tenant
  ├─ FirstName, LastName, Email, Phone
  └─ PrimaryDocId, AssignedNurseId (optional)

// Step 3: Patient auto-enrolled in default Program
Enrollment
  ├─ PatientId: {patient-id}
  ├─ ProgramId: {default-program-id}
  └─ EnrolledAt: DateTime.Now

// Step 4: PatientPathway created (Workflow starts)
PatientPathway
  ├─ TenantId: "customer-A"  // Same tenant
  ├─ PatientId: {patient-id}  // WHO
  ├─ EnrollmentId: {enrollment-id}  // WHICH program
  ├─ ProgramConfigurationId: "config-id"  // WHAT workflow
  ├─ CurrentProcessStageId: "stage-1-id"  // WHERE currently
  ├─ CurrentStatus: "Active"
  ├─ PatientType: "Chronic"  // Cohort classification
  ├─ PathwayData: { forms: [], responses: {} }  // Progress tracking
  └─ EnteredStageAt, LastTransitionAt: DateTime  // Timeline
```

**Result:** Patient automatically flows from intake → conversion → enrollment → workflow start

---

### 2️⃣ Program → ProgramConfiguration → Workflow Components

```
Tenant A (Customer A) Configuration Layer
│
├─ Program: "Basic Care Program" (Reusable template)
│  └─ ProgramConfiguration (Tenant-specific customization)
│     ├─ WorkflowType: "Linear"
│     ├─ PatientTypes: "Initial,Chronic,Advanced"
│     │
│     ├─ ProcessStages (What are the steps?)
│     │  ├─ Stage 1: "Initial Assessment" (DurationDays: 7)
│     │  │  └─ AllowedStatuses: "Active,Pending,Completed"
│     │  ├─ Stage 2: "Treatment Planning" (DurationDays: 14)
│     │  └─ Stage 3: "Follow-Up" (DurationDays: 30)
│     │
│     ├─ FormDefinitions (What forms to collect?)
│     │  ├─ Form 1: "Health Intake" → applies to Stage 1
│     │  │  ├─ Fields: [medicalHistory, medications, allergies]
│     │  │  └─ ApplicablePatientTypes: "Initial,Chronic"
│     │  └─ Form 2: "Treatment Approval" → applies to Stage 2
│     │
│     ├─ TaskDefinitions (What tasks to create?)
│     │  ├─ Task 1: "Nurse Assessment" → OnStageEnter Stage 1
│     │  └─ Task 2: "Monthly Check-up" → OnStageEnter Stage 3
│     │
│     ├─ BusinessRules (When to advance?)
│     │  ├─ Rule 1: OnFormSubmit → if form complete → transition Stage 2
│     │  └─ Rule 2: OnDaysPassed → if 7+ days in stage → alert
│     │
│     ├─ ContentTemplates (What messages to send?)
│     │  ├─ Template 1: Email "Assessment Due"
│     │  │  └─ "Hi {{patientName}}, your {{stageName}} is due by {{dueDate}}"
│     │  └─ Template 2: Email "Treatment Ready"
│     │
│     └─ RolePermissions (Who can do what?)
│        ├─ Role: "Patient"
│        │  ├─ Permissions: "Read,SubmitForms"
│        │  └─ AccessibleStages: [All stages]
│        ├─ Role: "Nurse"
│        │  ├─ Permissions: "Read,Create,Edit,Approve"
│        │  └─ AccessibleStages: [Stage 1, Stage 2]
│        └─ Role: "Admin"
│           ├─ Permissions: "Read,Create,Edit,Delete,Override"
│           └─ AccessibleStages: [All stages]
```

**Result:** Configuration fully defines workflow without any code changes

---

### 3️⃣ Patient Pathway State Transitions

```
Runtime State Machine:

NEW PATIENT (Post-Enrollment)
	│
	└─→ PatientPathway Created
		├─ CurrentProcessStageId = Stage 1 ID
		├─ CurrentStatus = "Active"
		├─ EnteredStageAt = Now
		└─ PathwayData = {} (empty)

STAGE 1: INITIAL ASSESSMENT (7 days)
	│
	├─ Required: Forms submitted?
	│  └─ Load FormDefinitions → "Health Intake"
	│  └─ Patient completes → Submit form response
	│
	├─ Evaluate: BusinessRules matching?
	│  └─ Rule: "OnFormSubmit AND formComplete → transition"
	│  └─ Check: All required fields present? ✅
	│  └─ Action: Execute transition
	│
	├─ Update: PathwayData JSON
	│  ├─ formResponses: { medicalHistory: "...", medications: "..." }
	│  ├─ completedForms: ["form-health-intake"]
	│  └─ lastFormSubmittedAt: DateTime
	│
	├─ Trigger: Side effects
	│  ├─ Create tasks: TaskDefinition for Stage 2
	│  ├─ Render template: "Treatment Plan Ready" email
	│  └─ Check permissions: Who can see next stage
	│
	├─ Transition: Move to next stage
	│  └─ CurrentProcessStageId = Stage 2 ID
	│  └─ LastTransitionAt = Now
	│  └─ CurrentStatus = "Active"
	│
	└─→ STAGE 2: TREATMENT PLANNING (14 days)
		├─ Load new forms: "Treatment Approval"
		├─ Load new tasks: "Nurse Review"
		├─ Check new permissions: Nurse can approve this stage
		├─ Evaluate new rules: "If approved → Stage 3"
		└─→ (Repeat process per stage)
			└─→ STAGE 3: FOLLOW-UP (30 days)
				├─ Final forms & tasks
				├─ Auto-discharge if complete
				└─→ DISCHARGE
					├─ CompletedAt = Now
					├─ CurrentStatus = "Completed"
					└─ PathwayData archived with full history
```

---

### 4️⃣ Multi-Tenant Isolation (Strict Boundary)

```
Tenant A Data
├─ TenantId: "8a3d9c2a-1111-..."
├─ Program Configurations: A1, A2, A3
├─ ProcessStages: S-A1-1, S-A1-2, S-A2-1, ...
├─ FormDefinitions: F-A1-Health, F-A2-Advanced, ...
├─ BusinessRules: R-A1-Auto, R-A2-Alert, ...
├─ Patients: Patient-A1, Patient-A2, ...
├─ PatientPathways: P-A1-Stage1, P-A2-Stage2, ...
└─ [CANNOT see Tenant B data]

┌─────────────────────── FIREWALL ───────────────────────┐

Tenant B Data
├─ TenantId: "xxxxxxxx-yyyy-..."  (Different!)
├─ Program Configurations: B1, B2
├─ ProcessStages: S-B1-1, S-B1-2, ...
├─ FormDefinitions: F-B1-Cardiac, ...
├─ BusinessRules: R-B1-Emergency, ...
├─ Patients: Patient-B1, Patient-B2, ...
├─ PatientPathways: P-B1-Stage1, ...
└─ [CANNOT see Tenant A data]

Every Query Pattern:
WHERE TenantId == requestTenantId  ← ENFORCED ALWAYS
```

**Result:** Tenant A and B are completely isolated despite sharing same database

---

### 5️⃣ Appointment Integration (Optional)

```
Appointment Links to Enrollment (optional):

Patient
  └─ Enrollment in Program A
	 ├─ Program: "Basic Care"
	 ├─ Status: Active
	 │
	 ├─ Appointment 1: "Initial Assessment"
	 │  ├─ EnrollmentId: {enrollment-id}  ← Links to program
	 │  ├─ ScheduledStart: 2025-01-10
	 │  └─ Can fetch: What workflow stage is patient in?
	 │
	 ├─ PatientPathway: Currently in Stage 2
	 │  ├─ CurrentProcessStageId: {stage-2-id}
	 │  ├─ StageName: "Treatment Planning"
	 │  └─ Forms pending: ["Treatment Approval"]
	 │
	 └─ Appointment 2: "Treatment Review"
		├─ EnrollmentId: {enrollment-id}
		└─ Appointment falls in Stage 2 of workflow
		   → Can auto-link appointment to patient's current workflow stage
```

**Result:** Appointments can be aware of patient's workflow progress (optional)

---

## Complete Entity Relationship Summary

| Entity | Connects To | Purpose | Tenant Scoped |
|--------|-------------|---------|--------------|
| **Lead** | Patient (via conversion) | Intake source | ✅ TenantId |
| **Patient** | Enrollment, Appointment | Master record | ✅ TenantId |
| **Program** | ProgramConfiguration | Core program template | ❌ Shared across tenants |
| **Enrollment** | Patient, Program, PatientPathway, Appointment | Links patient to program | ✅ Via Patient.TenantId |
| **Appointment** | Patient, Enrollment (optional) | Scheduling | ✅ TenantId |
| **ProgramConfiguration** | Program, ProcessStage, FormDef, TaskDef, BusinessRule, ContentTemplate, RolePermission | Defines workflow | ✅ TenantId |
| **ProcessStage** | ProgramConfiguration, FormDef, TaskDef, BusinessRule | Workflow steps | ✅ Via Config.TenantId |
| **FormDefinition** | ProgramConfiguration, ProcessStage | Data collection | ✅ Via Config.TenantId |
| **TaskDefinition** | ProgramConfiguration, ProcessStage | Automation | ✅ Via Config.TenantId |
| **BusinessRule** | ProgramConfiguration | Automation logic | ✅ Via Config.TenantId |
| **ContentTemplate** | ProgramConfiguration | Messages | ✅ Via Config.TenantId |
| **RolePermission** | ProgramConfiguration | Access control | ✅ Via Config.TenantId |
| **PatientPathway** | Patient, Enrollment, ProgramConfiguration, ProcessStage | Runtime state | ✅ TenantId |

---

## The Answer: Zero-Hardcoding Model

### Before (Hardcoded, Inflexible)
```csharp
if (customerId == "CUSTOMER123")
{
	stages = new[] { "Intake", "Assessment", "Treatment" };
	forms = new[] { new Form { Name = "Health Form", Fields = [...] } };

	if (stage == "Intake" && formSubmitted)
	{
		nextStage = "Assessment";
	}
}
```

### After (Database-Driven, Flexible)
```csharp
// Get tenant's configuration
var config = await db.ProgramConfigurations
	.Where(pc => pc.TenantId == tenantId && pc.ProgramId == programId)
	.FirstAsync();

// Get stages dynamically
var stages = await db.ProcessStages
	.Where(ps => ps.ProgramConfigurationId == config.Id)
	.OrderBy(ps => ps.Order)
	.ToListAsync();

// Get forms for current stage
var formsForStage = await db.FormDefinitions
	.Where(f => f.ProgramConfigurationId == config.Id
			 && f.ApplicableStages.Contains(currentStage.Name))
	.ToListAsync();

// Evaluate business rules
var applicableRules = await db.BusinessRules
	.Where(r => r.ProgramConfigurationId == config.Id
			 && r.TriggerEvent == "OnFormSubmit")
	.ToListAsync();

foreach (var rule in applicableRules)
{
	if (EvaluateCondition(rule.Condition, submission))
	{
		ExecuteAction(rule.Action);  // e.g., transition stage
	}
}
```

**Result:** Same code executes all customer workflows. Each customer defines their own via database.

---

## Testing the Connection (Quick Commands)

```bash
# 1. Create workflow configuration (admin)
POST /api/programconfiguration/create
{
  "programId": "11111111-2222-3333-4444-555555555555",
  "workflowType": "Linear"
}

# 2. Enroll patient (creates pathway)
POST /api/workflow/patients/{patientId}/enroll
{
  "enrollmentId": "{enrollment-id}",
  "programConfigurationId": "{config-id}",
  "patientType": "Chronic"
}

# 3. Get patient's workflow state
GET /api/workflow/pathways/{pathwayId}
Response:
{
  "id": "{pathwayId}",
  "patientId": "{patientId}",
  "enrollmentId": "{enrollmentId}",
  "programConfigurationId": "{configId}",
  "currentProcessStageId": "{stage-1-id}",
  "currentStatus": "Active",
  "pathwayData": {...}
}

# 4. Submit form (triggers workflow)
POST /api/workflow/pathways/{pathwayId}/submit-form
{
  "formId": "{form-id}",
  "responses": { "medicalHistory": "Diabetes", ... }
}

# 5. Check if ready to advance
GET /api/workflow/pathways/{pathwayId}/can-advance
Response:
{
  "canAdvance": true,
  "blockers": [],
  "nextStageName": "Treatment Planning",
  "reason": "All forms submitted and approved"
}

# 6. Advance to next stage
POST /api/workflow/pathways/{pathwayId}/transition-next
```

---

## Documentation Files for Deep Dives

| Document | Purpose | Read Time |
|----------|---------|-----------|
| README.md | Project overview & phases | 15 min |
| **WORKFLOW_CONNECTIONS.md** | ← **Start here for this question** | 20 min |
| WORKFLOW_ARCHITECTURE.md | Technical reference (APIs, entities) | 30 min |
| WORKFLOW_QUICK_REFERENCE.md | Cheat sheet & examples | 10 min |
| OUTSTANDING_WORK.md | Known issues & roadmap | 20 min |
| PHASE_3_5_COMPLETION_SUMMARY.md | Requirements validation | 15 min |
| DOCUMENTATION_INDEX.md | How to use all docs | 5 min |

---

## Summary

### Your Answer:

**Q: What is the connection between workflow and other entities?**

**A:** The workflow system ("WORKFLOW ENTITIES" layer) connects to the existing domain model through a three-step pipeline:

1. **Input Flow:** Lead → Patient → Enrollment (existing entities establish patient in program)
2. **Configuration Layer:** Patient's enrollment references a ProgramConfiguration, which defines all workflow components (stages, forms, rules, etc.) for that program
3. **Runtime Layer:** PatientPathway is created at enrollment, tracks patient's progression through the configured workflow stages
4. **Isolation:** Everything is scoped by TenantId, so each customer has their own programs, configurations, workflows, and patient pathways

**Result:** A zero-hardcoding system where every customer defines their own workflow in the database, and the same code executes all customer workflows without any customer-specific conditionals or hardcoded IDs.

✅ **Connection is complete and documented in detail.**
