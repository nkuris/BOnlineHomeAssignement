# Comprehensive Answer: Care Management System Architecture

**Topic:** Multi-tenant implementation with configurable workflows  
**Date:** September 2026  
**Language:** English

---

## 📋 Table of Contents

1. [Patient Participation in Multiple Programs](#1-patient-participation-in-multiple-programs)
2. [Patient in Multiple Tenants](#2-can-a-patient-appear-in-multiple-tenants)
3. [Timeline Ownership](#3-timeline-ownership)
4. [Nurse in Multiple Programs](#4-can-a-nurse-belong-to-multiple-programs)
5. [Tenant and Database](#5-shared-or-separate-database)
6. [Workflow Rules Storage & Execution](#6-how-workflow-rules-are-stored-and-executed)

---

## 1. Patient Participation in Multiple Programs

### ✅ **Answer: Yes, fully implemented**

#### Implementation:
- **Patient** (Patient.cs) - represents a single care recipient
- **Program** (Program.cs) - represents a care program
- **Enrollment** (Enrollment.cs) - represents the relationship between Patient and Program (junction table)

#### Entity Relationship:
```csharp
public class Patient
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }  // ← Scoped to single tenant
	public string FirstName { get; set; }
	public string LastName { get; set; }
	// Patient data...
}

public class Enrollment
{
	public Guid Id { get; set; }

	public Guid PatientId { get; set; }
	public Patient? Patient { get; set; }

	public Guid ProgramId { get; set; }
	public Program? Program { get; set; }

	public DateTime EnrolledAt { get; set; }
	public string? Status { get; set; }  // "Active", "Completed", "Paused", etc.
}
```

#### Real-World Example:
```
Patient: "John Smith" (ID: abc-123)
├── Enrollment #1 → Program: "Hypertension Management"
│   └── Status: "Active" (enrolled Sept 2026)
│
├── Enrollment #2 → Program: "Weight Loss Program"
│   └── Status: "Active" (enrolled Aug 2026)
│
└── Enrollment #3 → Program: "Cardiac Rehabilitation"
	└── Status: "Completed" (completed June 2026)
```

#### Benefits:
- ✅ **Flexibility** - Patient can be in multiple programs simultaneously
- ✅ **Independence** - Each program has its own workflow
- ✅ **Tracking** - Separate metrics and timeline for each enrollment

---

## 2. Can a Patient Appear in Multiple Tenants

### ❌ **Answer: No, not in current design**

#### Reason:
```csharp
public class Patient
{
	[Required]  // ← MANDATORY
	public Guid TenantId { get; set; }
}
```

- **TenantId is Required** - cannot be null
- **Each Patient belongs to exactly ONE Tenant**
- This is an **architectural choice** for data isolation

#### Tenant Isolation Benefits:
- ✅ **Security** - Data is inherently isolated per tenant
- ✅ **Compliance** - Simple to ensure HIPAA/GDPR compliance patterns
- ✅ **Simplicity** - No complex cross-tenant queries

#### If Multi-Tenant Support Needed:
Create a junction table:
```csharp
public class PatientTenantMapping
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }
	public Patient? Patient { get; set; }

	public Guid TenantId { get; set; }
	public Tenant? Tenant { get; set; }

	public DateTime AssignedAt { get; set; }
	public bool IsActive { get; set; }
}
```

---

## 3. Timeline Ownership

### ✅ **Answer: Timeline belongs to PatientPathway**

#### Structure:

```csharp
public class PatientPathway
{
	public Guid Id { get; set; }

	[Required]
	public Guid PatientId { get; set; }
	public Patient? Patient { get; set; }

	[Required]
	public Guid EnrollmentId { get; set; }
	public Enrollment? Enrollment { get; set; }

	[Required]
	public Guid ProgramConfigurationId { get; set; }

	public Guid CurrentProcessStageId { get; set; }
	public ProcessStage? CurrentProcessStage { get; set; }

	public DateTime EnteredStageAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	// ... other workflow tracking fields
}

public class WorkflowEvent
{
	public Guid Id { get; set; }

	[Required]
	public Guid PatientPathwayId { get; set; }  // ← Direct link to pathway
	public PatientPathway? PatientPathway { get; set; }

	[Required]
	public string EventType { get; set; }  
	// "Enrolled", "StageTransitioned", "FormSubmitted", "TaskCompleted", etc.

	[Required]
	public string Description { get; set; }

	[Required]
	public DateTime EventOccurredAt { get; set; }

	public bool VisibleToPatient { get; set; }
	public string? EventData { get; set; }  // JSON payload
}
```

#### Hierarchy Example:
```
Patient: "Sarah Cohen"
└── Enrollment: "Diabetes Management Program"
	└── PatientPathway (represents patient's progression through program)
		├── Timeline Event 1: "Enrolled in program" (Sept 1)
		├── Timeline Event 2: "Moved to Initial Assessment Stage" (Sept 5)
		├── Timeline Event 3: "Form submitted" (Sept 8)
		├── Timeline Event 4: "Moved to Treatment Planning Stage" (Sept 15)
		└── Timeline Event 5: "Moved to Monitoring Stage" (Oct 1)
```

#### Key Insight:
- **One Patient** → **Many Enrollments** → **Many PatientPathways** → **Many WorkflowEvents**
- Each path through a program has its own **independent timeline**
- **Different enrollments = different timelines**, even for the same patient and program

---

## 4. Can a Nurse Belong to Multiple Programs

### ✅ **Answer: Yes, through RolePermissions**

#### Structure:
```csharp
public class RolePermission
{
	public Guid Id { get; set; }

	[Required]
	public Guid TenantId { get; set; }

	[Required]
	public Guid ProgramConfigurationId { get; set; }  // ← Different per program

	[Required]
	[MaxLength(100)]
	public string RoleName { get; set; }  // "Doctor", "Nurse", "CareCoordinator", etc.

	[MaxLength(500)]
	public string? AccessibleStages { get; set; }  // CSV: "Stage1,Stage2,Stage3"

	[MaxLength(500)]
	public string? Permissions { get; set; }  // CSV: "Read,Create,Edit,Delete,Approve"

	public string? AssignableTaskTypes { get; set; }  // Types of tasks this role can assign

	public bool CanViewPathways { get; set; }
	public bool CanApproveTransitions { get; set; }
	public bool CanOverrideRules { get; set; }
}
```

#### Real-World Scenario:
```
Staff Member: "Lisa Johnson" (Nurse ID: nurse-456)

RolePermission #1:
├── Program: "Hypertension Management"
├── Accessible Stages: "Assessment,Monitoring"
├── Permissions: "Read,Create,Edit"
└── Can Approve Transitions: true

RolePermission #2:
├── Program: "Cardiac Rehabilitation"
├── Accessible Stages: "Recovery,PhysicalTherapy"
├── Permissions: "Read,Create"
└── Can Approve Transitions: false

RolePermission #3:
├── Program: "Diabetes Management"
├── Accessible Stages: "Assessment,Education"
├── Permissions: "Read"
└── Can Approve Transitions: false
```

#### Benefits:
- ✅ **Flexible staffing** - One nurse can support multiple programs
- ✅ **Role variation** - Different permissions per program
- ✅ **Scalable** - Easy to add/remove program assignments

---

## 5. Shared or Separate Database

### 🔷 **Answer: Shared Database with Tenant Data Isolation**

#### Implementation:
```csharp
// Every domain entity includes TenantId
public class Patient { public Guid TenantId { get; set; } }
public class Enrollment { public Guid TenantId { get; set; } }  // via Patient relationship
public class ProcessStage { public Guid TenantId { get; set; } }
public class WorkflowEvent { public Guid TenantId { get; set; } }
public class BusinessRule { public Guid TenantId { get; set; } }
// ... and so on for ALL entities
```

#### Query Safety Pattern:
```csharp
// ❌ UNSAFE - exposes all data:
var patients = await context.Patients.ToListAsync();

// ✅ SAFE - filters by tenant:
var patients = await context.Patients
	.Where(p => p.TenantId == currentTenantId)
	.ToListAsync();

// ✅ BETTER - if using multitenancy middleware:
public class TenantContext
{
	public Guid CurrentTenantId { get; set; }
}

// DbContext helper:
var patients = await context.Patients
	.Where(p => p.TenantId == _tenantContext.CurrentTenantId)
	.ToListAsync();
```

#### Advantages:
- ✅ **Cost efficient** - One database for all tenants
- ✅ **Easy maintenance** - Single schema migration
- ✅ **Quick onboarding** - New tenants share existing schema

#### Disadvantages:
- ❌ **Requires discipline** - Must filter by TenantId in every query
- ❌ **Query bugs** - A missing `.Where()` can leak cross-tenant data
- ❌ **Backup complexity** - Restoring one tenant is more complex

---

## 6. How Workflow Rules Are Stored and Executed

### ✅ **Answer: BusinessRule + TriggerExecution Pattern**

#### A. Storage: BusinessRule Table

```csharp
public class BusinessRule
{
	public Guid Id { get; set; }

	[Required]
	public Guid TenantId { get; set; }

	[Required]
	public Guid ProgramConfigurationId { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; }  // e.g., "Auto-advance high scorers"

	public string? Description { get; set; }

	[MaxLength(50)]
	public string RuleType { get; set; }  
	// "StageTransition", "TaskTrigger", "FormVisibility", "Escalation", "Notification"

	[MaxLength(100)]
	public string TriggerEvent { get; set; }  
	// "OnFormSubmit", "OnStageEntry", "Daily", "OnDayX"

	[Required]
	public string Condition { get; set; }  // JSON-serialized logic

	[Required]
	public string Action { get; set; }  // JSON-serialized action

	public int Priority { get; set; }  // 1=highest
	public bool IsActive { get; set; }
}
```

#### Condition and Action Format (JSON):

```json
{
  "rule": "Auto-advance chronic patients with high compliance",
  "condition": {
	"type": "and",
	"conditions": [
	  {
		"type": "PatientTypeMatch",
		"field": "patientType",
		"operator": "equals",
		"value": "Chronic"
	  },
	  {
		"type": "FormScoreThreshold",
		"field": "mostRecentFormScore",
		"operator": "greaterThan",
		"value": 80
	  }
	]
  },
  "action": {
	"type": "StageTransition",
	"targetStageId": "stage-uuid-final-monitoring",
	"notificationMessage": "Congratulations! You've progressed to the Monitoring stage.",
	"sendNotification": true
  }
}
```

#### B. Execution: TriggerExecution Table

```csharp
public class TriggerExecution
{
	public Guid Id { get; set; }

	[Required]
	public Guid TenantId { get; set; }

	[Required]
	public Guid PatientPathwayId { get; set; }  // ← Running FOR this patient in this enrollment

	[Required]
	[MaxLength(100)]
	public string TriggerType { get; set; }
	// "TimeBasedFollowUp", "EventBasedFormResponse", "QuestionnaireSchedule"

	public string? TriggerConfigId { get; set; }  // Links to rule or trigger config

	[MaxLength(50)]
	public string Status { get; set; }  // "Pending", "Processing", "Succeeded", "Failed"

	public int AttemptCount { get; set; }
	public DateTime? ExecutedAt { get; set; }
	public string? ErrorMessage { get; set; }

	public DateTime CreatedAt { get; set; }
}
```

#### C. Full Execution Flow:

```
STEP 1: Event Occurs in System
└─ Patient submits intake form for Stage 1

STEP 2: System Triggers Rules Evaluation
├─ Query: SELECT * FROM BusinessRules 
│  WHERE ProgramConfigurationId = @programId 
│  AND TriggerEvent = "OnFormSubmit" 
│  AND IsActive = true 
│  ORDER BY Priority DESC
└─ Found: 3 matching rules

STEP 3: For Each Rule, Create TriggerExecution
├─ INSERT TriggerExecution (PatientPathwayId, TriggerType, Status="Pending")
└─ Creates 3 records to track evaluation

STEP 4: Evaluate Each Rule's Condition
├─ Rule 1: "Auto-advance if score > 80"
│  └─ Check: mostRecentFormScore = 85 ✓ TRUE
│
├─ Rule 2: "Send escalation if score < 40"
│  └─ Check: mostRecentFormScore = 85 ✗ FALSE (skip action)
│
└─ Rule 3: "Flag for doctor review if chronic + score > 70"
   └─ Check: patientType = "Chronic" ✓ AND score = 85 ✓ TRUE

STEP 5: Execute Matched Actions
├─ Rule 1 Action: Transition to Stage 2
│  └─ UPDATE PatientPathway SET CurrentProcessStageId = @stageId
│
└─ Rule 3 Action: Create task and notification
   ├─ INSERT TaskItem (AssignToRole="Doctor")
   └─ Send notification to care team

STEP 6: Record Execution
├─ UPDATE TriggerExecution SET Status="Succeeded", ExecutedAt=NOW()
└─ Each rule logs its attempt count and success status

STEP 7: Create Timeline Event
└─ INSERT WorkflowEvent
   ├─ EventType: "TriggerFired"
   ├─ Description: "Automatically advanced to Stage 2 (score: 85)"
   ├─ TriggeredBy: "Trigger:RuleId1"
   └─ VisibleToPatient: true
```

#### D. Complete Example in Code:

```csharp
// 1. Define the rule
var advancementRule = new BusinessRule
{
	Name = "Auto-advance high compliance chronic patients",
	RuleType = "StageTransition",
	TriggerEvent = "OnFormSubmit",
	Priority = 1,
	IsActive = true,

	Condition = JsonConvert.SerializeObject(new
	{
		type = "and",
		conditions = new[] {
			new { field = "patientType", op = "equals", value = "Chronic" },
			new { field = "formScore", op = "greaterThan", value = 80 }
		}
	}),

	Action = JsonConvert.SerializeObject(new
	{
		type = "transition",
		targetStageId = finalMonitoringStageId,
		message = "You've advanced to Monitoring stage",
		notifyPatient = true
	})
};

await context.BusinessRules.AddAsync(advancementRule);
await context.SaveChangesAsync();

// 2. When form is submitted, trigger execution
var formSubmissionEvent = new WorkflowEvent
{
	PatientPathwayId = patientPathwayId,
	EventType = "FormSubmitted",
	Description = "Intake form submitted",
	EventOccurredAt = DateTime.UtcNow
};

await context.WorkflowEvents.AddAsync(formSubmissionEvent);

// 3. Background job or middleware evaluates rules
var applicableRules = await context.BusinessRules
	.Where(r => r.ProgramConfigurationId == programId 
			 && r.TriggerEvent == "OnFormSubmit"
			 && r.IsActive)
	.OrderBy(r => r.Priority)
	.ToListAsync();

foreach (var rule in applicableRules)
{
	var execution = new TriggerExecution
	{
		PatientPathwayId = patientPathwayId,
		TriggerType = "EventBasedFormResponse",
		Status = "Pending"
	};

	try
	{
		// Evaluate condition
		bool conditionMet = EvaluateCondition(
			rule.Condition, 
			patientPathway, 
			formData
		);

		if (conditionMet)
		{
			// Execute action
			ExecuteAction(rule.Action, patientPathway);
			execution.Status = "Succeeded";
		}
		else
		{
			execution.Status = "Skipped";  // Condition not met
		}
	}
	catch (Exception ex)
	{
		execution.Status = "Failed";
		execution.ErrorMessage = ex.Message;
	}

	execution.ExecutedAt = DateTime.UtcNow;
	context.TriggerExecutions.Update(execution);
}

await context.SaveChangesAsync();
```

---

## 📊 Entity Relationship Diagram

```
┌─────────────────────────────┐
│ TENANT                      │
│ (Organization boundary)     │
└──────────────┬──────────────┘
			   │
		┌──────┴────────┬─────────────────┐
		│               │                 │
	┌───▼──────┐  ┌─────▼──────┐  ┌──────▼──────┐
	│ PATIENT  │  │ PROGRAM    │  │ USER/NURSE  │
	│  (P1)    │  │  (Prog A)  │  │  (N1)       │
	└───┬──────┘  └─────┬──────┘  └──────┬──────┘
		│               │                │
		│         ┌─────▼────────────┐   │
		│         │ PROGRAM CONFIG   │   │
		│         │  (Workflow def)  │   │
		│         └──────┬──────────┘   │
		│                │              │
	┌───▼────────────┐   │         ┌────▼─────────────┐
	│ ENROLLMENT     │   │         │ ROLE PERMISSION  │
	│ (P1→Prog A)   │   │         │ (N1→Prog A perms)│
	└────┬──────────┘   │         └──────────────────┘
		 │              │
	┌────▼──────────────────────┐
	│ PATIENT PATHWAY           │
	│ (P1's journey in Prog A)  │
	└────┬─────────────────────┘
		 │
	┌────┴─────┬──────────┬──────────┐
	│           │          │          │
┌───▼──┐  ┌────▼───┐  ┌───▼────┐  ┌──▼───┐
│EVENTS│  │BUSINESS│  │TRIGGER │  │FORMS │
│      │  │ RULES  │  │EXECURI │  │      │
│      │  │        │  │ON      │  │      │
└──────┘  └────────┘  └────────┘  └──────┘
```

---

## 🔒 Security Patterns

```csharp
// 1. Tenant isolation in every query
public IQueryable<Patient> GetSecurePatients(Guid tenantId)
{
	return context.Patients
		.Where(p => p.TenantId == tenantId);  // ← MANDATORY FILTER
}

// 2. Role-based access within program
var userPrograms = await context.RolePermissions
	.Where(rp => rp.UserId == userId && rp.TenantId == tenantId)
	.Select(rp => rp.ProgramConfigurationId)
	.ToListAsync();

// 3. Verify user can access pathway
var pathway = await context.PatientPathways
	.FirstOrDefaultAsync(pp => 
		pp.Id == pathwayId 
		&& pp.TenantId == userTenantId
		&& userPrograms.Contains(pp.ProgramConfigurationId));

if (pathway == null) 
	throw new UnauthorizedAccessException();

// 4. Audit trail
await context.AuditLogs.AddAsync(new AuditLog
{
	UserId = userId,
	TenantId = tenantId,
	Action = "ViewedPatientData",
	EntityId = patientId,
	Timestamp = DateTime.UtcNow
});
```

---

## 📚 Code References

- `Patient.cs` - Patient entity with TenantId
- `Enrollment.cs` - Enrollment junction table
- `PatientPathway.cs` - Track patient progression (part of WorkflowEntities.cs)
- `WorkflowEvent.cs` - Timeline events (part of WorkflowEntities.cs)
- `BusinessRule.cs` - Workflow rules (part of WorkflowEntities.cs)
- `TriggerExecution.cs` - Rule execution tracking (part of WorkflowEntities.cs)
- `RolePermission.cs` - Role-program permissions (part of WorkflowEntities.cs)
- `AppDbContext.cs` - EF Core mapping and relationships

---

**This document serves as a comprehensive explanation of the system's architecture for validation and review.**
