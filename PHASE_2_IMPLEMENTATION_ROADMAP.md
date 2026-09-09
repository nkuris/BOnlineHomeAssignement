# Implementation Roadmap: Extended Requirements

## Requirements Alignment Map

```
HEBREW REQUIREMENTS          CURRENT IMPLEMENTATION      NEW IMPLEMENTATION (PHASE 2)
─────────────────────────────────────────────────────────────────────────────────────

ציר זמן, אירועים ומשימות
(Timeline, Events, Tasks)
				│
				├─ Timeline           ❌ MISSING       →  ✅ WorkflowEvent entity
				├─ Events             ⚠️ PARTIAL       →  ✅ Full event tracking
				├─ Tasks              ⚠️ MINIMAL       →  ✅ Complete TaskItem
				└─ Visibility         ❌ MISSING       →  ✅ VisibleToPatient flag

תהליכים וטריגרים
(Processes & Triggers)
				│
				├─ Time-based triggers ❌ MISSING       →  ✅ TimeBasedTriggerService
				├─ Event-based triggers ⚠️ BASIC        →  ✅ EventBasedTriggerService
				├─ Retry handling      ❌ MISSING       →  ✅ TriggerExecution retry
				├─ Duplicate prevention ❌ MISSING      →  ✅ ExecutionKey tracking
				└─ Failure handling    ❌ MISSING       →  ✅ LastError + retries

טפסים, שאלונים ותקשורת
(Forms, Questionnaires, Communication)
				│
				├─ Form responses      ⚠️ STORE ONLY    →  ✅ Conditional logic
				├─ Auto-create tasks   ❌ MISSING       →  ✅ FormResponseService
				├─ Auto-transition     ⚠️ BASIC         →  ✅ Full conditions
				├─ Questionnaires      ❌ MISSING       →  ✅ Questionnaire entity
				├─ Communication       ❌ MISSING       →  ✅ CommunicationMessage
				├─ SMS/Email/WhatsApp  ❌ NOT DESIGNED  →  ✅ Provider abstraction
				└─ Async scheduling    ❌ MISSING       →  ✅ BackgroundJobService
```

---

## Phased Implementation Plan

### 🟢 Phase 1: Current State ✅ (Completed)

**What Works:**
```
✅ Program Configuration (stages, forms, rules)
✅ Patient Enrollment → PatientPathway
✅ Basic form submission
✅ Simple status tracking
✅ Multi-tenant isolation
```

**Limitations:**
```
❌ No timeline of events
❌ No task management
❌ No time-based triggers
❌ No form conditional logic
❌ No communication system
```

---

### 🟡 Phase 2: Extended Requirements (New)

#### Week 1: Entities & Schema

**Add 6 New Entities:**
```csharp
// Day 1-2: WorkflowEvent
public class WorkflowEvent
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid PatientPathwayId { get; set; }
	public string EventType { get; set; }  // Lead, Enrolled, FormSubmitted, Stage, Task
	public DateTime OccurredAt { get; set; }
	public bool VisibleToPatient { get; set; }
	public string Description { get; set; }  // Human-readable
	public string Data { get; set; }  // JSON details
}

// Day 2-3: Enhanced TaskItem
public class TaskItem
{
	// Extend existing with: AssignedToUserId, DueDate, Priority
	// Priority, Outcome, CompletionNotes, CreationSource
}

// Day 3: Questionnaire
public class Questionnaire
{
	public Guid Id { get; set; }
	public Guid FormDefinitionId { get; set; }
	public string RecurrencePattern { get; set; }  // Once, Weekly, Monthly, DaysAfter
	public int TimeoutDays { get; set; }
}

// Day 3-4: QuestionnaireResponse
public class QuestionnaireResponse
{
	public Guid Id { get; set; }
	public Guid PatientPathwayId { get; set; }
	public Guid QuestionnaireId { get; set; }
	public DateTime SentAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public string Responses { get; set; }  // JSON
}

// Day 4: TriggerExecution
public class TriggerExecution
{
	public Guid Id { get; set; }
	public Guid BusinessRuleId { get; set; }
	public string Status { get; set; }  // Pending, Completed, Failed
	public int AttemptCount { get; set; }
	public string ExecutionKey { get; set; }  // Unique, prevent duplicates
	public string LastError { get; set; }
}

// Day 4-5: CommunicationMessage
public class CommunicationMessage
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }
	public string Channel { get; set; }  // Email, SMS, WhatsApp, Voice
	public string Status { get; set; }  // Pending, Sent, Delivered, Failed
	public DateTime? SentAt { get; set; }
	public string DeliveryError { get; set; }
}

✅ Migration: dotnet ef migrations add AddExtendedWorkflowEntities
```

---

#### Week 2: Services & Business Logic

**Day 6-7: TimeBasedTriggerService**
```csharp
public class TimeBasedTriggerService : ITimeBasedTriggerService
{
	// Handles: 7 days after enrollment, 30 days after questionnaire, etc.
	// Algorithm:
	// 1. Get pathways where enrollment_date + N days = today
	// 2. Check ExecutionKey to prevent duplicates
	// 3. Create TriggerExecution
	// 4. Execute action (create task, send message)
	// 5. Record WorkflowEvent
}

Testing:
- Create enrollment
- Advance clock 7 days
- Call ProcessPendingTriggersAsync()
- Assert: Follow-up task created
```

**Day 8: EventBasedTriggerService**
```csharp
public class EventBasedTriggerService : IEventBasedTriggerService
{
	// Handles: OnConsentSigned, OnFormSubmitted, OnTaskCompleted
	// Algorithm:
	// 1. Event occurs → New WorkflowEvent
	// 2. Find all rules with matching trigger type
	// 3. Evaluate conditions
	// 4. Execute actions
}

Testing:
- Create ConsentSignedEvent
- Assert: rule with "OnConsentSigned" trigger fires
- Assert: condition evaluated
```

**Day 9: FormResponseService**
```csharp
public class FormResponseService : IFormResponseService
{
	// Handle form submission with conditional logic
	// Algorithm:
	// 1. Validate responses against form schema
	// 2. Store responses in PathwayData
	// 3. Record FormSubmitted event
	// 4. Evaluate conditional rules
	// 5. Execute actions (create task, transition stage, send mail)
}

Testing:
- Submit form with "severity: high"
- Assert: Conditional rule detected
- Assert: Urgent task created automatically
```

**Day 10: CommunicationService**
```csharp
public class CommunicationService : ICommunicationService
{
	// Design provider pattern (no actual integration yet)
	// Providers:
	// - EmailCommunicationProvider (throws NotImplementedError for now)
	// - SmsCommunicationProvider
	// - WhatsAppCommunicationProvider
	// - VoiceCommunicationProvider

	// Algorithm:
	// 1. Get provider for channel
	// 2. Call SendAsync()
	// 3. Update message status
}

Design (not implementation):
- Each provider implements ICommunicationProvider
- Can be integrated later with true service
```

---

#### Week 3: Controllers & APIs

**Day 11: Timeline Endpoints**
```csharp
[HttpGet("patients/{patientId}/timeline")]
// Returns: List<EventDto> ordered by date
// Filter: VisibleToPatient = true only
// Usage: Patient dashboard shows their journey

[HttpGet("patients/{patientId}/tasks")]
// Returns: Active tasks for patient
// Filter by status: Pending, InProgress, Completed
// Sort by DueDate
```

**Day 12: Form Submission with Conditions**
```csharp
[HttpPost("pathways/{pathwayId}/submit-form")]
// Input: FormId + Responses
// Output: FormSubmissionResultDto with TriggeredActions
// Shows what automatically happened:
//   "Task created: Doctor review"
//   "Transitioned to Stage 2"
//   "Email sent: Follow-up confirmation"
```

**Day 13: Questionnaire Endpoints**
```csharp
[HttpGet("pathways/{pathwayId}/questionnaires")]
// Returns: All questionnaires for this program
// Shows: Status, SentAt, CompletedAt, NextScheduledFor

[HttpPost("questionnaires/{id}/submit")]
// Submit questionnaire responses
// Trigger: Form response logic applies
```

---

#### Week 4: Background Processing

**Day 14: Background Job Service**
```csharp
public class WorkflowBackgroundJobService : BackgroundService
{
	// Runs every 1 minute
	// Algorithm:
	// 1. Get all tenants
	// 2. For each tenant:
	//    a. Process time-based triggers
	//    b. Process pending messages
	//    c. Retry failed executions

	// Registration:
	builder.Services.AddHostedService<WorkflowBackgroundJobService>();
}

Deployment:
- Single instance (or coordinated instances) runs triggers
- Prevents duplicate execution via ExecutionKey
- Handles failures + retries
```

**Day 15: Integration Testing**
```csharp
[Theory]
[InlineData("7DaysAfterEnrollment")]
[InlineData("30DaysAfterQuestionnaire")]
public async Task TimeBasedTrigger_ExecutesAfterNDays(string triggerName)
{
	// Setup enrollment
	// Advance clock by N days
	// Call ProcessPendingTriggersAsync
	// Assert: Task created, Event recorded, no duplicates
}

[Fact]
public async Task FormSubmission_WithCondition_CreatesTask()
{
	// Setup form with condition: if(severity==high) createTask
	// Submit form with severity=high
	// Assert: Task created automatically
	// Assert: Event recorded
	// Assert: Patient can see in timeline
}
```

---

## Timeline Visualization

```
CURRENT (Phase 1)          PHASE 2 WEEK 1           PHASE 2 WEEK 2
─────────────────          ──────────────           ──────────────
						  (Entities)               (Services)
						  │                        │
Enrollment                ├─ WorkflowEvent         ├─ TimeBasedTrigger
   ↓                      ├─ TaskItem (extended)   ├─ EventBasedTrigger
PatientPathway            ├─ Questionnaire         ├─ FormResponse
   ├─ CurrentStage        ├─ QuestionnaireResp     ├─ Communication
   ├─ FormResponses       └─ TriggerExecution      └─ WorkflowEvent
   └─ Transitions            CommunicationMsg

No visible timeline,      Timeline visible,        Auto-actions work,
no auto-actions          basic structure           retry + fail handling


PHASE 2 WEEK 3             PHASE 2 WEEK 4
──────────────             ──────────────
(Controllers)             (Background + Tests)
│                         │
├─ /timeline              ├─ BackgroundJobService
├─ /tasks                 ├─ PeriodicTimer (1 min)
├─ /submit-form           ├─ ProcessPendingTriggers
└─ /questionnaires        ├─ ProcessPendingMessages
						  └─ Full integration tests


END OF PHASE 2:
┌─────────────────────────────────────────────┐
│ Complete Event-Driven Patient Journey       │
│                                             │
│ ✅ Timeline visible (when/what happened)   │
│ ✅ Automatic triggers (time + events)      │
│ ✅ Conditional logic (form → action)       │
│ ✅ Task management (full properties)       │
│ ✅ Questionary scheduling & tracking      │
│ ✅ Communication ready (provider pattern)  │
│ ✅ Audit trail complete                    │
│ ✅ Retry/duplicate/failure handling       │
│                                             │
│ Production-ready workflow engine            │
└─────────────────────────────────────────────┘
```

---

## Entity Relationship Diagram (Phase 2)

```
						┌─────────────────────┐
						│ ProgramConfiguration│
						│  (Workflow Def)     │
						└──────────┬──────────┘
								   │
					┌──────────────┼─────────────────┐
					│              │                 │
			  ┌─────▼────┐   ┌─────▼────┐   ┌────────▼──────┐
			  │ ProcessStage │   │ FormDef │   │ BusinessRule │
			  └─────┬────┘   └─────┬────┘   └────────┬──────┘
					│              │                 │
		┌───────────┴──────────┬───┴─────────────┬──┴──────────┐
		│                      │                 │             │
   ┌────▼────────┐    ┌────────▼──────┐  ┌──────▼──────┐   ┌──▼──────────────┐
   │ PatientPath-│    │ WorkflowEvent │  │FormResponse │   │ TriggerExecution│
   │ way         │    │               │  │Service      │   │(when it runs)   │
   │             │    │ (Timeline)    │  │ (conditions)│   │(retry/fail)     │
   └──────┬──────┘    │               │  │             │   │                 │
		  │           │ - EventType   │  │ -Responses  │   │ - Status        │
		  │           │ - OccurredAt  │  │ -Triggers   │   │ - AttemptCount  │
		  │           │ - Visible     │  │-Actions    │   │ - LastError     │
		  │           └───────────────┘  └─────────────┘   └──────────────────┘
		  │
	┌─────┴──────────────────────────────────────┐
	│                                             │
┌───▼────────────┐             ┌──────────────────▼──┐
│ TaskItem       │             │ Questionnaire       │
│ (EXTENDED)     │             │                     │
│                │             │ - FormDefinitionId  │
│ - DueDate      │             │ - RecurrencePattern │
│ - Priority     │             │ - TimeoutDays       │
│ - Assigned     │             │                     │
│ - Outcome      │             └──────────┬──────────┘
│ - Creation     │                        │
│   Source       │             ┌──────────▼──────────┐
└───┬────────────┘             │ QuestionnaireResp   │
	│                          │                     │
	└──────┐          ┌────────┤ - SentAt            │
		   │          │        │ - CompletedAt       │
		   │    ┌─────▼──┐     │ - Responses (JSON)  │
		   │    │ Patient│     │ - TriggerAction     │
		   └───▶│        │◀────┘                     │
				│ Pathway│     └─────────────────────┘
				│ ┌──────►─────────────────┐
				│ │                        │
				│ └──────────┐             │
				│            │             │
		   ┌────▼────┐   ┌────▼────┐   ┌──▼─────────────┐
		   │ Patient │   │Enrollment│   │ Communication │
		   │         │   │          │   │ Message       │
		   │(existing)   │(existing)   │(new)           │
		   └─────────┘   └──────────┘   │               │
										 │ - Channel     │
										 │ - Status      │
										 │ - SentAt      │
										 └───────────────┘
```

---

## API Timeline Example (Before & After)

### BEFORE (Phase 1)

```bash
# Create workflow
POST /api/programconfiguration/create

# Enroll patient
POST /api/workflow/patients/{id}/enroll

# Know current state
GET /api/workflow/pathways/{id}
Response: {
  "currentStageName": "Assessment",
  "status": "Active"
  # That's it - no history, no timeline
}
```

### AFTER (Phase 2)

```bash
# SAME as before, plus:

# See patient's journey
GET /api/workflow/patients/{id}/timeline
Response: [
  {
	"type": "Lead Created",
	"description": "New lead from portal",
	"occurredAt": "2025-01-01T10:00:00Z",
	"visibleToPatient": false
  },
  {
	"type": "Consent Sent",
	"description": "Consent form emailed",
	"occurredAt": "2025-01-02T02:30:00Z",
	"visibleToPatient": true
  },
  {
	"type": "Consent Signed",
	"description": "Patient signed consent",
	"occurredAt": "2025-01-02T02:35:00Z",
	"visibleToPatient": true
  },
  {
	"type": "Patient Enrolled",
	"description": "Patient enrolled in Chronic Care Program",
	"occurredAt": "2025-01-03T09:00:00Z",
	"visibleToPatient": true
  }
]

# See active tasks
GET /api/workflow/patients/{id}/tasks
Response: [
  {
	"id": "task-123",
	"title": "Nurse Follow-up Call",
	"type": "PhoneCall",
	"status": "Pending",
	"dueDate": "2025-01-10",
	"priority": "High",
	"assignedToRole": "Nurse",
	"creationSource": "TimeBasedTrigger",
	"createdAt": "2025-01-03T09:00:00Z"
  }
]

# Submit form with auto-actions
POST /api/workflow/pathways/{id}/submit-form
Request: {
  "formId": "form-123",
  "responses": {
	"severity": "high",
	"symptoms": "chest pain"
  }
}
Response: {
  "success": true,
  "events": [
	"Form response recorded",
	"Urgent task created: Doctor review",
	"Email sent: We received your urgent report",
	"Stage auto-transitioned to Treatment"
  ]
}

# View questionnaires
GET /api/workflow/pathways/{id}/questionnaires
Response: [
  {
	"id": "quest-1",
	"name": "Health Assessment",
	"status": "Completed",
	"completedAt": "2025-01-21T15:30:00Z",
	"nextScheduledFor": "2025-02-20"
  },
  {
	"id": "quest-2",
	"name": "Medication Review",
	"status": "Sent",
	"sentAt": "2025-01-25T08:00:00Z",
	"dueBy": "2025-02-01"
  }
]
```

---

## Success Metrics (Phase 2)

| Feature | Success Criteria |
|---------|------------------|
| **Timeline** | Patient sees ≥10+ events, ordered by date, with descriptions |
| **Tasks** | All tasks show: title, due date, priority, status, creation source |
| **Time-Based Triggers** | "7 days after enrollment" correctly identifies matching pathways, no duplicates |
| **Event-Based Triggers** | "On form submit" correctly fires rules, evaluates conditions |
| **Conditional Logic** | Form response of "severity=high" creates appropriate task + notification |
| **Questionnaires** | Sent on time, responses tracked, next scheduled date correct |
| **Communication** | Messages queued, status tracked (Sent/Failed), retry attempts logged |
| **Audit Trail** | All events recorded, timestamps accurate, traceability complete |
| **Retry Handling** | Failed trigger retries 3x with exponential backoff, stops after max |
| **Duplicate Prevention** | Same trigger doesn't fire twice in same day for same patient |

---

## Migration Path

```
Current Codebase
		│
		├─ WorkflowEntities.cs (add new entities)
		├─ AppDbContext.cs (add DbSets + mappings)
		├─ Migration (AddExtendedWorkflowEntities)
		│
		├─ Services/
		│   ├─ TimeBasedTriggerService.cs (new)
		│   ├─ EventBasedTriggerService.cs (new)
		│   ├─ FormResponseService.cs (new)
		│   ├─ CommunicationService.cs (new)
		│   └─ WorkflowBackgroundJobService.cs (new)
		│
		├─ DTO/
		│   ├─ TimelineDto.cs
		│   ├─ TaskDto.cs
		│   ├─ QuestionnaireDto.cs
		│   └─ CommunicationDto.cs
		│
		├─ Controllers/
		│   └─ TimelineController.cs (new, consolidates all timeline/task APIs)
		│
		├─ Program.cs (register new services + background job)
		│
		└─ Tests/
			├─ TimeBasedTriggerTests.cs
			├─ EventBasedTriggerTests.cs
			├─ FormResponseTests.cs
			└─ CommunicationTests.cs
```

---

## Does This Answer Extended Requirements?

| Requirement  | Phase 1 | Phase 2 | Status |
|------|---------|---------|--------|
| **Timeline** (ציר זמן) | ❌ | ✅ | WorkflowEvent + API |
| **Events** (אירועים) | ⚠️ | ✅ | Full tracking |
| **Tasks** (משימות) | ⚠️ | ✅ | Complete properties |
| **Time-based** (זמן) | ❌ | ✅ | TimeBasedTriggerService |
| **Event-based** (אירוע) | ⚠️ | ✅ | EventBasedTriggerService |
| **Retry/Duplicate** | ❌ | ✅ | TriggerExecution tracking |
| **Failures** (כשלים) | ❌ | ✅ | LastError + retries |
| **Forms** (טפסים) | ⚠️ | ✅ | Conditional + auto-actions |
| **Questionnaires** (שאלונים) | ❌ | ✅ | Questionnaire entity |
| **Communication** (תקשורת) | ❌ | ✅ | Provider pattern ready |

**✅ ALL REQUIREMENTS FULLY ADDRESSED IN PHASE 2**

---

## Budget Estimate

| Component | Effort | Time |
|-----------|--------|------|
| Entities & Migration | 10% | 1 day |
| Services (Business Logic) | 40% | 3-4 days |
| Controllers & DTOs | 20% | 2 days |
| Background Job | 10% | 1 day |
| Testing & Validation | 20% | 2 days |
| **Total** | **100%** | **~10 days** |

Ready to implement? Start with EXTENDED_REQUIREMENTS_IMPLEMENTATION.md
