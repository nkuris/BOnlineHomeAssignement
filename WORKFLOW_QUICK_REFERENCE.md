# Workflow Architecture Quick Reference

## TL;DR (2-Minute Overview)

**What we built:** A customer-configurable healthcare workflow engine where every tenant defines their own care programs, stages, forms, tasks, rules, and permissions — **all in the database, zero code changes**.

**How it works:**
1. Admin defines workflow → Create `ProgramConfiguration` (stages, forms, rules)
2. Patient enrolls → Create `PatientPathway` (tracks current stage, form responses)
3. Patient progresses → Transitions between stages based on form submissions and business rules
4. All automated → Rules engine evaluates conditions and triggers actions (transitions, tasks, notifications)

**Key entities:**
```
Database Configuration (Setup Time)
├─ ProgramConfiguration: "What is the workflow?"
├─ ProcessStage: "What are the steps?" (e.g., Assessment → Treatment → Follow-up)
├─ FormDefinition: "What forms to collect?" 
├─ TaskDefinition: "What tasks to create?"
├─ BusinessRule: "When to advance?" (e.g., if form submitted → transition)
├─ ContentTemplate: "What messages to send?" (email/SMS)
└─ RolePermission: "Who can do what?" (Patient, Nurse, Admin)

Runtime (Patient-Specific)
└─ PatientPathway: "Where is this patient?" (current stage, form responses, status)
```

**Result:** No hardcoded customer IDs, all customer-specific configuration per TenantId in database.

---

## Architecture Diagram

```
TENANT A (Customer A)
│
├─ Program: "Basic Care"
│  └─ ProgramConfiguration (Config A)
│     ├─ Stages: Assessment → Treatment → Follow-up
│     ├─ Forms: Health Intake, Treatment Approval
│     ├─ Rules: Auto-advance if form + nurse approved
│     ├─ Templates: "Your assessment is due", "Treatment plan ready"
│     └─ Roles: Patient (read), Nurse (create), Admin (override)
│
├─ Patient: John Doe
│  └─ Enrollment in Program "Basic Care"
│     └─ PatientPathway (tracks progression)
│        ├─ Current Stage: Treatment
│        ├─ Form Responses: { medicalHistory: "...", medications: "..." }
│        ├─ Status: Active
│        └─ Assigned Tasks: Monthly Check-up
│
└─ Patient: Jane Smith
   └─ Enrollment in Program "Basic Care"
	  └─ PatientPathway (independent from John's)
		 ├─ Current Stage: Assessment
		 ├─ Form Responses: { ... pending ... }
		 └─ Status: Awaiting Form Submission


TENANT B (Customer B)
│
├─ Program: "Advanced Care"
│  └─ ProgramConfiguration (Config B) ← Different from Config A
│     ├─ Stages: Initial → Advanced → Discharge
│     ├─ Forms: Detailed Assessment, Imaging Review
│     ├─ Rules: Different rules than Tenant A
│     └─ ... completely separate from Tenant A ...
│
└─ (Tenant B's patients progress through Tenant B's workflow)

STRICT ISOLATION: Tenant A data never visible to Tenant B
```

---

## Entity Relationship Guide

### Lead → Patient → Workflow Journey

```csharp
// 1. LEAD ARRIVES (from external source)
var lead = new Lead {
	Id = Guid.NewGuid(),
	TenantId = tenantId,  // Who does this lead belong to?
	Email = "john@example.com",
	PhoneNumber = "555-1234"
};

// 2. LEAD CONVERTS TO PATIENT
var patient = new Patient {
	Id = Guid.NewGuid(),
	TenantId = tenantId,  // Same tenant
	FirstName = "John",
	LastName = "Doe",
	Email = lead.Email
};

// 3. PATIENT AUTO-ENROLLED IN DEFAULT PROGRAM
var enrollment = new Enrollment {
	Id = Guid.NewGuid(),
	PatientId = patient.Id,
	ProgramId = defaultProgram.Id,  // Default program for this tenant
	EnrolledAt = DateTime.UtcNow
};

// 4. PATHWAY CREATED (Workflow starts)
var pathway = new PatientPathway {
	Id = Guid.NewGuid(),
	TenantId = tenantId,                           // Same tenant
	PatientId = patient.Id,                        // Who is progressing?
	EnrollmentId = enrollment.Id,                  // Which program?
	ProgramConfigurationId = config.Id,            // What workflow?
	CurrentProcessStageId = stage1.Id,             // Starting at Stage 1
	CurrentStatus = "Active",
	PatientType = "Chronic",                       // Patient cohort
	PathwayData = "{forms:[...], responses:[...]}" // Progression JSON
};

// 5. PATIENT COMPLETES STAGE 1 FORM
var submission = new { formId: "...", responses: { medicalHistory: "..." } };

// 6. BUSINESS RULE TRIGGERED
// Rule: "If form submitted AND all fields populated → advance"
if (/* condition met */) {
	// Action: Transition to Stage 2
	pathway.CurrentProcessStageId = stage2.Id;
	pathway.CurrentStatus = "Pending";
	pathway.LastTransitionAt = DateTime.UtcNow;
}
```

### Configuration Entities (Setup-Time)

```csharp
// 1. Create program workflow configuration
var config = new ProgramConfiguration {
	Id = Guid.NewGuid(),
	TenantId = customerTenantId,
	ProgramId = basicCareProgram.Id,
	WorkflowType = "Linear",  // Linear, Branching, or Custom
	PatientTypes = "Initial,Chronic,Advanced"
};

// 2. Define workflow stages
var stage1 = new ProcessStage {
	ProgramConfigurationId = config.Id,
	Name = "Initial Assessment",
	Order = 1,
	DurationDays = 7,
	AllowedStatuses = "Active,Pending,Completed"
};

// 3. Attach forms to stage
var form = new FormDefinition {
	ProgramConfigurationId = config.Id,
	Title = "Health Intake",
	ApplicableStages = "Initial Assessment",
	FieldsDefinition = "[{name: 'medicalHistory', type: 'textarea'}, ...]"
};

// 4. Define automation rules
var rule = new BusinessRule {
	ProgramConfigurationId = config.Id,
	Name = "Auto-advance on form submit",
	TriggerEvent = "OnFormSubmit",  // When does it fire?
	Condition = "{formId: '...'}",   // Which form?
	Action = "{type: 'transition', toStageOrder: 2}"  // What to do?
};

// 5. Create notification templates
var template = new ContentTemplate {
	ProgramConfigurationId = config.Id,
	TemplateName = "Assessment Due",
	Channel = "Email",
	Content = "Hi {{patientName}}, your {{stageName}} is due by {{dueDate}}"
};

// 6. Grant role permissions
var rolePerms = new RolePermission {
	ProgramConfigurationId = config.Id,
	RoleName = "Nurse",
	Permissions = "Read,Create,Edit,Approve",
	AccessibleStages = "Initial Assessment,Treatment Planning"
};
```

---

## API Endpoints Quick Map

### Admin Configuration APIs (Setup)
```
POST   /api/programconfiguration/create              → Create program workflow
GET    /api/programconfiguration/{id}                → Get configuration details

POST   /api/programconfiguration/{id}/stages         → Add process stage
GET    /api/programconfiguration/{id}/stages         → List stages
PUT    /api/programconfiguration/{id}/stages/{sid}   → Update stage
DELETE /api/programconfiguration/{id}/stages/{sid}   → Remove stage

POST   /api/programconfiguration/{id}/forms          → Add form definition
GET    /api/programconfiguration/{id}/forms          → List forms
POST   /api/programconfiguration/{id}/rules          → Add business rule
POST   /api/programconfiguration/{id}/templates      → Add message template
POST   /api/programconfiguration/{id}/roles          → Define role permissions

GET    /api/programconfiguration/{id}/export         → Export config as JSON
POST   /api/programconfiguration/{id}/import         → Import config from JSON
```

### Clinical Workflow APIs (Runtime)
```
POST   /api/workflow/patients/{id}/enroll            → Start patient in workflow
GET    /api/workflow/patients/{id}/pathways          → List patient's pathways

GET    /api/workflow/pathways/{id}                   → Get current pathway state
GET    /api/workflow/pathways/{id}/required-forms    → Forms for current stage
POST   /api/workflow/pathways/{id}/submit-form       → Submit form (triggers rules)
GET    /api/workflow/pathways/{id}/tasks             → Check assigned tasks

GET    /api/workflow/pathways/{id}/can-advance       → Check if ready to next stage
POST   /api/workflow/pathways/{id}/transition-next   → Auto-advance
POST   /api/workflow/pathways/{id}/transition-to/{stageid}  → Jump to stage (admin)
POST   /api/workflow/pathways/{id}/update-status     → Change status within stage

POST   /api/workflow/pathways/{id}/complete          → Discharge from pathway
```

---

## Data Flow Example: Patient Submits Form

```
1. USER ACTION
   └─ Patient fills "Health Assessment" form
	  └─ Submits: { medicalHistory: "Diabetes", medications: "Metformin" }

2. API RECEIVES
   └─ POST /api/workflow/pathways/{id}/submit-form
	  └─ Header: X-Tenant-Id: 8a3d9c2a-...

3. WORKFLOW SERVICE
   └─ Save form responses to PatientPathway.PathwayData JSON
   └─ Emit FormSubmitted event
   └─ Evaluate all BusinessRules matching:
	  - TriggerEvent = "OnFormSubmit"
	  - MatchingFormId = "health-assessment-id"

4. BUSINESS RULE ENGINE
   └─ Check condition: "Are all required fields filled?"
	  └─ ✅ Yes (medicalHistory + medications present)
   └─ Execute action: "Transition to next stage"

5. STATE UPDATES
   └─ Load current stage: "Initial Assessment"
   └─ Load next stage: "Treatment Planning" (Order 2)
   └─ Update PathwayData:
	  {
		"currentStageId": "22222222-...",
		"formResponses": { "medicalHistory": "Diabetes", ... },
		"lastTransitionAt": "2025-01-04T...",
		"transitionReason": "Auto-advanced via business rule"
	  }
   └─ Set CurrentStatus = "Active"

6. SIDE EFFECTS
   └─ Load forms for "Treatment Planning" stage
	  └─ Require: "Treatment Approval" form
   └─ Load tasks for new stage
	  └─ Create: "Nurse Review" task
   └─ Load content template: "Treatment Plan Ready"
	  └─ Render with variables:
		 "Hi {{patientName}}, your {{stageName}} is ready. Complete by {{dueDate}}"
   └─ Queue email notification

7. RESPONSE
   └─ Returns updated PatientPathway:
	  {
		"id": "{id}",
		"currentStageName": "Treatment Planning",
		"currentStatus": "Active",
		"requiredForms": ["treatment-approval-form-id"],
		"assignedTasks": ["nurse-review-task-id"],
		"pathwayData": { ... // Full progression history
	  }
```

---

## Multi-Tenant Isolation Pattern

### The Rule: Always Filter by TenantId

```csharp
// ✅ CORRECT: Every query filters by TenantId
public async Task<PatientPathway> GetPatientPathwayAsync(
	Guid tenantId,      // ← Required!
	Guid pathwayId)
{
	return await db.PatientPathways
		.Where(p => p.TenantId == tenantId)  // ← Always check
		.FirstOrDefaultAsync(p => p.Id == pathwayId);
}

// ❌ WRONG: Missing TenantId filter
public async Task<PatientPathway> GetPatientPathwayAsync(Guid pathwayId)
{
	return await db.PatientPathways
		.FirstOrDefaultAsync(p => p.Id == pathwayId);  // ← No tenant check!
}

// ❌ RESULT: Tenant A could access Tenant B's patient data
```

### Multi-Tenant Request Header
```bash
# Every request includes tenant header
curl -X POST "/api/workflow/pathways/{id}/submit-form" \
	 -H "X-Tenant-Id: 8a3d9c2a-1111-..." \  # ← Tenant identification
	 -H "Content-Type: application/json"
```

### Response: Complete Isolation

```
Tenant A (Customer A)
├─ ProgramConfiguration: A1, A2, A3
├─ ProcessStages: A1-S1, A1-S2, A2-S1, ...
├─ PatientPathways: A-P1 (John → A1-S2), A-P2 (Jane → A1-S1)
└─ (Cannot see Tenant B's anything)

Tenant B (Customer B)
├─ ProgramConfiguration: B1, B2
├─ ProcessStages: B1-S1, B1-S2, B2-S1, ...
├─ PatientPathways: B-P1 (Bob → B1-S1), B-P2 (Alice → B2-S2)
└─ (Cannot see Tenant A's anything)
```

---

## Workflow Progression State Machine

```
PATIENT PATHWAY STATES:

New Enrollment
	↓
+─────────────────────────────────────┐
│       ProcessStage 1 (Order=1)      │
│   "Initial Assessment" (7 days)     │
│                                     │
│  Required Forms:                    │
│  ├─ Health Intake (pending)         │
│                                     │
│  Status Transitions:                │
│  ├─ Active (entered stage)          │
│  ├─ Pending (awaiting approval)     │
│  └─ Completed (move to next stage)  │
│                                     │
│  Business Rules:                    │
│  ├─ Auto-advance if form filled     │
│  ├─ Alert if overdue 3+ days        │
│  └─ Send reminder at 5 days         │
└─────────────────────────────────────┘
	↓ (Auto-transition via rule)
+─────────────────────────────────────┐
│       ProcessStage 2 (Order=2)      │
│   "Treatment Planning" (14 days)    │
│                                     │
│  Required Forms:                    │
│  ├─ Treatment Approval (pending)    │
│                                     │
│  Status: Active → Pending           │
│  Assigned Tasks: Nurse Review       │
└─────────────────────────────────────┘
	↓ (Admin transition)
+─────────────────────────────────────┐
│       ProcessStage 3 (Order=3)      │
│        "Follow-Up" (30 days)        │
│                                     │
│  Status: Active → Completed         │
└─────────────────────────────────────┘
	↓
PATHWAY COMPLETE
	↓
Discharge Date Set
Patient Moved to Archived
```

---

## Testing Checklist

- [ ] Admin creates ProgramConfiguration
- [ ] Admin adds ProcessStages (order, duration, statuses)
- [ ] Admin adds FormDefinition to stage
- [ ] Admin creates BusinessRule (trigger + condition + action)
- [ ] Patient enrolls in program → PathwayPathway created with Stage 1
- [ ] Get required forms for Stage 1 → form returned
- [ ] Submit form responses → PathwayData JSON updated
- [ ] BusinessRule evaluates → Check condition ✅
- [ ] Action executes → Transition to Stage 2
- [ ] Verify CurrentProcessStageId changed
- [ ] Get required forms for Stage 2 → new form returned
- [ ] Admin transitions to Stage 3 (override)
- [ ] Complete pathway → CompletedAt timestamp set

---

## Production Checklist

- [ ] All entities have TenantId and filtered queries
- [ ] X-Tenant-Id header required on all endpoints
- [ ] Audit logging captures all transitions
- [ ] Migration tested and applied to production database
- [ ] BusinessRule JSON validation in place
- [ ] ContentTemplate variable substitution tested
- [ ] FormDefinition field validation working
- [ ] RolePermission checks enforced on all operations
- [ ] Timestamps accurate for SLA tracking
- [ ] Error handling for malformed JSON configs
- [ ] Documentation accessible to customers

---

## One-Liner Summary

**Database-driven, tenant-scoped, zero-code healthcare workflow engine supporting customer-defined care programs, patient pathways, forms, tasks, rules, and permissions.**
