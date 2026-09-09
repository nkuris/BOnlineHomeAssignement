# Workflow Entity Relationships & Connections

## Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         EXISTING ENTITIES                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Lead ─────┐        Patient ─────┐        Enrollment ─────┐           │
│            │            │         │            │           │           │
│  (Intake)  └──→ (Convert) ───────→│    (Links Patient to  │           │
│                                   │     Program)          │           │
│  Appointment ◄─────────────────────┘                       │           │
│     │                                                      │           │
│     └──────────────────────────┬──────────────────────────┘           │
│                                │                                       │
│                        (Optional Link)                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
								  │
					   (TenantId Scoping)
								  │
								  ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    NEW WORKFLOW ENTITIES (Phase 3.5)                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Program ──────┐                                                       │
│                │                                                       │
│                ▼                                                       │
│    ProgramConfiguration ◄──────────────────────┐                      │
│     (Workflow Definition)                      │                      │
│                │                               │                      │
│    ┌───────────┼───────────┬────────────┬──────┴─────────┐            │
│    │           │           │            │                 │           │
│    ▼           ▼           ▼            ▼                 ▼           │
│ ProcessStage  FormDef   TaskDef    ContentTemplate   RolePermission   │
│    │           │         │              │                 │           │
│    │           │         │              │                 │           │
│    │ (Defines  │ (Data   │ (Auto  │ (Messages  │ (Access  │           │
│    │  stages)  │ collect) │ tasks)  │  & vars)  │  control) │        │
│    │           │         │         │            │           │        │
│    └───────────┼─────────┼─────────┼────────────┘           │        │
│                │         │         │                         │        │
│                └─────────┼─────────┴────────────────┐        │        │
│                          │                         │        │        │
│                          ▼                         ▼ (Optional)      │
│                   BusinessRule ◄────────────────────────────┘        │
│                   (Automation Logic)                                  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
								  │
					(TenantId: strict isolation)
								  │
								  ▼
┌──────────────────────────────────────────────────────────────────────┐
│                      PatientPathway (Runtime)                         │
│                 (Tracks patient progress through workflow)            │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Links to:                                                           │
│  ├─ Patient (WHO is progressing)                                    │
│  ├─ Enrollment (WHICH program they're in)                           │
│  ├─ ProgramConfiguration (WHAT workflow they follow)                │
│  │   └─┬─ ProcessStage (WHERE they are - current stage)             │
│  │     ├─ FormDefinition (WHAT forms to complete)                  │
│  │     ├─ TaskDefinition (WHAT tasks are triggered)                │
│  │     ├─ ContentTemplate (WHAT messages are sent)                 │
│  │     ├─ BusinessRule (WHAT automation applies)                   │
│  │     └─ RolePermission (WHO can access/modify)                   │
│  │                                                                   │
│  └─ Stores: PathwayData JSON (form responses, decisions)             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

## Connection Map: From Lead to Workflow

### 1. Lead Intake → Patient Creation → Workflow Enrollment

```csharp
// Step 1: Lead captured from supplier
var lead = new Lead
{
	Id = Guid.NewGuid(),
	TenantId = tenantId,
	LeadSource = "ReferralPartner",
	SupplierPayload = {...supplier-specific JSON...}
};
db.Leads.Add(lead);

// Step 2: Lead converted to Patient
var patient = new Patient
{
	Id = Guid.NewGuid(),
	TenantId = tenantId,
	FirstName = lead.FirstName,
	LastName = lead.LastName,
	Email = lead.Email,
	Phone = lead.Phone
};
db.Patients.Add(patient);

// Step 3: Patient enrolled in Program
var enrollment = new Enrollment
{
	Id = Guid.NewGuid(),
	PatientId = patient.Id,
	ProgramId = programId,  // ← Links to Program
	EnrolledAt = DateTime.UtcNow
};
db.Enrollments.Add(enrollment);

// Step 4: Create PatientPathway using ProgramConfiguration
var pathway = new PatientPathway
{
	Id = Guid.NewGuid(),
	TenantId = tenantId,
	PatientId = patient.Id,           // ← Who
	EnrollmentId = enrollment.Id,     // ← Enrollment link
	ProgramConfigurationId = progConfigId,  // ← Workflow definition
	CurrentProcessStageId = stage1.Id,      // ← Starting stage
	CurrentStatus = "Active",
	PatientType = "Chronic",          // ← Cohort classification
	EnteredStageAt = DateTime.UtcNow
};
db.PatientPathways.Add(pathway);
```

### 2. Entity Relationship Details

#### A. Lead → Patient → Enrollment → Workflow

```
Lead.TenantId = Patient.TenantId = Enrollment.TenantId = PatientPathway.TenantId
┌─────────────┬──────────────┬───────────────┬─────────────────┐
│   Lead      │   Patient    │   Enrollment  │  PatientPathway │
├─────────────┼──────────────┼───────────────┼─────────────────┤
│ Id          │ Id ◄─ Link   │ Id            │ Id              │
│ TenantId    │ TenantId     │ TenantId      │ TenantId        │
│ LeadSource  │ FirstName    │ PatientId ────┼─ PatientId      │
│ Email       │ LastName     │ ProgramId     │ EnrollmentId ◄──┤─ Link
│ ...         │ Email        │ EnrolledAt    │ ProgramConfigId │
│             │ ...          │ Status        │ CurrentStageId  │
│             │              │               │ PathwayData     │
│             │              │               │ CurrentStatus   │
│             │              │               │ IsActive        │
└─────────────┴──────────────┴───────────────┴─────────────────┘
```

#### B. Program → ProgramConfiguration → Components

```
Program (Core Care Program)
	│
	├─ Id = "11111111-2222-3333-4444-555555555555"
	├─ Name = "Basic Care Program"
	├─ IsActive = true
	│
	└──→ ProgramConfiguration (Customization Layer)
		 │
		 ├─ Id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
		 ├─ TenantId = "{customer's tenant}"
		 ├─ WorkflowType = "Linear"
		 ├─ PatientTypes = "Initial,Advanced,Chronic"
		 │
		 ├──→ ProcessStages (Workflow Steps)
		 │    ├─ Stage 1: "Initial Assessment" (Order=1, DurationDays=7)
		 │    ├─ Stage 2: "Treatment Planning" (Order=2, DurationDays=14)
		 │    └─ Stage 3: "Follow-Up" (Order=3, DurationDays=30)
		 │
		 ├──→ FormDefinitions (Data Collection)
		 │    ├─ "Health Intake" → applies to Stage 1
		 │    └─ "Treatment Approval" → applies to Stage 2
		 │
		 ├──→ TaskDefinitions (Automation Templates)
		 │    ├─ "Nurse Assessment" → triggered at Stage 1
		 │    └─ "Monthly Check-up" → triggered at Stage 3
		 │
		 ├──→ ContentTemplates (Messaging)
		 │    ├─ Email: "Assessment Due" → {{patientName}}, {{dueDate}}
		 │    └─ Email: "Treatment Plan Ready" → {{stageName}}
		 │
		 ├──→ BusinessRules (Automation Logic)
		 │    ├─ "Auto-advance if form submitted + nurse approved"
		 │    └─ "Alert provider if stage overdue > 3 days"
		 │
		 └──→ RolePermissions (Access Control)
			  ├─ Patient: Read, SubmitForms (all stages)
			  ├─ Nurse: Read, Create, Edit (all stages), CanApproveTransitions
			  └─ Admin: Read, Create, Edit, Delete, CanOverrideRules
```

#### C. PatientPathway → Current State Snapshot

At any moment, a PatientPathway holds:

```csharp
new PatientPathway
{
	// Identity
	Id = Guid.Parse("..." ),
	TenantId = Guid.Parse("8a3d9c2a-1111-..."),  // Customer
	PatientId = Guid.Parse("..." ),              // WHO
	EnrollmentId = Guid.Parse("..." ),           // Which enrollment

	// Configuration Reference
	ProgramConfigurationId = Guid.Parse("aaaaaaaa-bbbb-..."),  // WHAT workflow

	// Current State
	CurrentProcessStageId = Guid.Parse("11111111-aaaa-..."),   // Stage 1
	CurrentStatus = "Active",                                   // In-stage status
	PatientType = "Chronic",                                    // Cohort

	// Timeline
	EnteredStageAt = DateTime.UtcNow.AddDays(-5),              // Entered Stage 1
	StageDueAt = DateTime.UtcNow.AddDays(2),                   // Due in 2 days
	LastTransitionAt = DateTime.UtcNow.AddDays(-5),            // Last change
	CompletedAt = null,                                        // Not yet discharged

	// Data
	PathwayData = """
	{
		"completedForms": ["form-health-intake"],
		"formResponses": {
			"medicalHistory": "Type 2 diabetes, hypertension",
			"medications": "Metformin 500mg BID"
		},
		"lastFormSubmittedAt": "2025-01-04T..."
	}
	""",

	// Status
	IsActive = true,
	CreatedAt = DateTime.UtcNow.AddDays(-5)
};
```

## Data Flow: How Entities Connect

### Scenario 1: Patient Forms Submitted → Auto-Advance

```mermaid
graph TD
	A[Patient submits form] -->|OnFormSubmit event| B[WorkflowService.OnFormSubmittedAsync]
	B -->|Store responses| C[PatientPathway.PathwayData JSON]
	B -->|Evaluate rules| D[Check BusinessRule with TriggerEvent='OnFormSubmit']
	D -->|Condition matches| E["Action: Transition to next stage"]
	E -->|Fetch current stage| F[ProcessStage]
	F -->|Get next stage| G[ProcessStage Order+1]
	G -->|Update pathway| H[PatientPathway.CurrentProcessStageId = Stage2]
	H -->|Check required forms| I[Get FormDefinitions for Stage2]
	I -->|Create tasks| J[Trigger TaskDefinitions for Stage2]
	J -->|Send notifications| K[Render ContentTemplate with {{variables}}]
	K -->|Result| L[Patient receives email about new stage]
```

### Scenario 2: Role-Based Access & Permissions

```csharp
// Nurse accessing a patient's pathway
var tenantId = Guid.Parse("8a3d9c2a-1111-...");  // Customer
var nurseRole = "Nurse";
var pathwayId = Guid.Parse("..." );

// Get the pathway
var pathway = await db.PatientPathways.FindAsync(pathwayId);

// Get the program config
var progConfig = await db.ProgramConfigurations
	.FindAsync(pathway.ProgramConfigurationId);

// Check if Nurse can access this stage
var rolePerms = await db.RolePermissions
	.Where(rp => rp.TenantId == tenantId
			 && rp.ProgramConfigurationId == progConfig.Id
			 && rp.RoleName == nurseRole)
	.FirstOrDefaultAsync();

if (rolePerms == null || !rolePerms.AccessibleStages.Contains(pathway.CurrentProcessStageId.ToString()))
{
	throw new UnauthorizedAccessException("Nurse cannot access this stage");
}

// Get current stage details
var currentStage = await db.ProcessStages
	.FindAsync(pathway.CurrentProcessStageId);

// Get forms required at this stage
var requiredForms = await db.FormDefinitions
	.Where(f => f.ProgramConfigurationId == progConfig.Id
			 && f.ApplicableStages.Contains(currentStage.Name))
	.ToListAsync();

// Can nurse create tasks?
if (rolePerms.Permissions.Contains("Create"))
{
	// Allowed to create tasks
}
```

## Multi-Tenant Isolation

**Every workflow entity includes `TenantId`:**

```csharp
// Query for one tenant only
var pathways = await db.PatientPathways
	.Where(p => p.TenantId == tenantId)
	.ToListAsync();

var configs = await db.ProgramConfigurations
	.Where(pc => pc.TenantId == tenantId)
	.ToListAsync();

var forms = await db.FormDefinitions
	.Where(f => f.TenantId == tenantId)
	.ToListAsync();

// Result: Complete isolation between customers
// Tenant A cannot see Tenant B's configurations, pathways, or data
```

## Connection Summary Table

| Entity | Connects To | Purpose | Multi-Tenant |
|--------|------------|---------|--------------|
| **ProgramConfiguration** | Program, TenantId | Defines workflow for a program | Yes (TenantId) |
| **ProcessStage** | ProgramConfiguration | Stages in workflow | Yes (via ProgramConfig) |
| **PatientPathway** | Patient, Enrollment, ProgramConfiguration, ProcessStage | Tracks patient progress | Yes (TenantId) |
| **FormDefinition** | ProgramConfiguration, ProcessStage (via name match) | Forms at stages | Yes (TenantId) |
| **TaskDefinition** | ProgramConfiguration, ContentTemplate, ProcessStage (via TriggeredTaskIds) | Auto-tasks | Yes (TenantId) |
| **ContentTemplate** | ProgramConfiguration, TaskDefinition (optional) | Messages | Yes (TenantId) |
| **BusinessRule** | ProgramConfiguration, ProcessStage/Form/Template (in JSON) | Automation | Yes (TenantId) |
| **RolePermission** | ProgramConfiguration, ProcessStage (via AccessibleStages) | Access control | Yes (TenantId) |

## Key Design Patterns

### 1. Configuration → Runtime State Mapping

```
┌──────────────────────────────────────────────┐
│   CONFIGURATION LAYER (Setup-time)           │
│   - ProgramConfiguration                     │
│   - ProcessStage definitions                 │
│   - FormDefinition templates                 │
│   - BusinessRule logic                       │
│   - ContentTemplate text                     │
│   - RolePermission restrictions              │
└──────────────┬───────────────────────────────┘
			   │
		(when patient enrolls)
			   │
			   ▼
┌──────────────────────────────────────────────┐
│   RUNTIME LAYER (Patient-specific)           │
│   - PatientPathway created                   │
│   - CurrentProcessStageId set                │
│   - PathwayData stores form responses        │
│   - Transitions & status updates             │
│   - Task instances created from definitions  │
└──────────────────────────────────────────────┘
```

### 2. Forms → Responses → Data Flow

```csharp
// Configuration: Form Definition
var formDef = new FormDefinition
{
	Id = Guid.Parse("44444444-..."),
	Title = "Health Assessment",
	FieldsDefinition = """[
		{"name":"conditions","type":"checkbox"},
		{"name":"medications","type":"textarea"}
	]"""
};

// Runtime: Patient submits
var submission = new FormSubmissionRequest
{
	FormId = formDef.Id,
	Responses = new Dictionary<string, object>
	{
		["conditions"] = new[] { "Diabetes", "Hypertension" },
		["medications"] = "Metformin 500mg"
	}
};

// Stored in PatientPathway
pathway.PathwayData = JsonSerializer.Serialize(new
{
	completedForms = new[] { formDef.Id },
	formResponses = submission.Responses,
	lastFormSubmittedAt = DateTime.UtcNow
});
```

### 3. Rules → Actions → State Changes

```csharp
// Configuration: Business Rule
var rule = new BusinessRule
{
	Name = "Auto-advance on form submit",
	TriggerEvent = "OnFormSubmit",
	Condition = JsonDocument.Parse("""{"formId":"44444444-..."}"""),
	Action = JsonDocument.Parse("""{"type":"transition","toStage":"22222222-..."}""")
};

// Runtime: Form submitted
// → WorkflowService.OnFormSubmittedAsync()
// → WorkflowService.EvaluateBusinessRulesAsync()
// → Check rule.TriggerEvent == "OnFormSubmit" ✓
// → Evaluate rule.Condition ✓
// → Execute rule.Action
//   → WorkflowService.TransitionToNextStageAsync()
//   → Update PatientPathway.CurrentProcessStageId
//   → Fetch new stage tasks
//   → Update PatientPathway.CurrentStatus
```

## Appointment ↔ Workflow Connection (Optional)

Appointments can optionally link to Enrollment workflows:

```csharp
public class Appointment
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }        // Always linked to patient
	public Guid? EnrollmentId { get; set; }    // Optional: links to program
	public string Title { get; set; }
	public DateTime ScheduledStart { get; set; }
	// ... other appointment fields
}

// When creating appointment for an enrolled patient:
var appointment = new Appointment
{
	PatientId = patient.Id,
	EnrollmentId = enrollment.Id,  // ← Links appointment to program workflow
	Title = "Nurse Assessment",
	ScheduledStart = DateTime.UtcNow.AddDays(3)
};

// Later: Can fetch pathway context for appointment
var pathway = await db.PatientPathways
	.FirstAsync(p => p.EnrollmentId == appointment.EnrollmentId);
// Now can see which stage of workflow this appointment belongs to
```

## Summary

**The workflow system sits between Programs (business configuration) and Patients (individuals):**

```
Program ──→ ProgramConfiguration ──→ ProcessStage
				  ↓                       ↓
			RolePermission          PatientPathway ←─ Patient
				  ↓                       ↓
			FormDefinition         (current state)
			TaskDefinition          
			ContentTemplate         
			BusinessRule
```

**Key connections:**
1. **Configuration Time**: Define program → customize with stages, forms, rules, templates
2. **Enrollment Time**: Patient joins → create pathway in this configuration
3. **Runtime**: Patient progresses → pathway tracks stage, forms, tasks, status
4. **Access**: Roles control who can access which stages and perform which actions
5. **Automation**: Rules evaluate form submissions, trigger transitions, send notifications
6. **Multi-Tenant**: Every entity scoped by `TenantId` for strict isolation

All workflow entities are **configuration-driven, zero-hardcoding, and fully auditable**.
