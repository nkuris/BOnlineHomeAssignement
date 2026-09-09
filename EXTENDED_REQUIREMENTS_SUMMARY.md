# Extended Requirements Response: Summary

## Your Requirements (Translated from Hebrew)

You provided 3 additional requirement sections:

### 1. **ציר זמן, אירועים ומשימות** (Timeline, Events & Tasks)
> Each patient has a sequence of events and actions within the program. Timeline includes: business events, tasks, user actions, forms, calls, messages, questionnaire responses, status changes.

### 2. **תהליכים וטריגרים** (Processes & Triggers)
> System must support automatic actions triggered by time or events. Must distinguish between two types and handle: Retry, duplicates, failures, timing.

### 3. **טפסים, שאלונים ותקשורת** (Forms, Questionnaires & Communication)
> Programs include: Registration forms, Consent, Medical Intake, Questionnaires, Follow-up forms. Form responses can affect pathway progression, create new tasks, or trigger communication.

---

## How We Respond

We've created **Phase 2** implementation plan that **completely addresses all extended requirements**.

---

## Documents Provided

### 1. **EXTENDED_REQUIREMENTS_IMPLEMENTATION.md** (Detailed Design)
- Complete entity definitions with all properties
- Service implementations with full code
- Examples of every requirement
- API endpoint specifications

**Size:** ~4000 lines, covers everything from entities to API contracts

### 2. **PHASE_2_IMPLEMENTATION_ROADMAP.md** (Visual Planning)
- 4-week implementation schedule
- By-day breakdown of work
- Success metrics for each feature
- Budget estimate (10 days total)

**Size:** ~2000 lines, week-by-week guidance

### 3. **PHASE_2_QUICK_START.md** (Executable Guide)
- Step-by-step implementation instructions
- Ready-to-copy code for each phase
- Database migration commands
- Testing examples

**Size:** ~1500 lines, action-oriented

---

## What Gets Built (Phase 2)

### New Entities (6)

```
✅ WorkflowEvent
   └─ Timeline of all patient events
   └─ Visibility control (some events only for staff)
   └─ Timestamp + description for each event

✅ Questionnaire
   └─ Template for forms
   └─ Recurrence pattern (once, weekly, monthly, days after event)
   └─ Timeout for completion

✅ QuestionnaireResponse
   └─ Track when questionnaire sent, opened, completed
   └─ Store patient's responses
   └─ Record what actions were triggered

✅ TriggerExecution
   └─ Track when/if business rules execute
   └─ Prevent duplicate execution (ExecutionKey)
   └─ Handle retries (AttemptCount, NextRetryAt)
   └─ Record failures (LastError)

✅ CommunicationMessage
   └─ Prepare messages for: Email, SMS, WhatsApp, Voice
   └─ Track delivery status
   └─ Handle failures + retries

✅ TaskItem (EXTEND)
   └─ Add: DueDate, Priority, AssignedUser
   └─ Add: Outcome, CompletionNotes
   └─ Add: CreationSource (time-triggered vs event-triggered)
```

### New Services (4)

```
✅ TimeBasedTriggerService
   └─ "7 days after enrollment → Create Follow-up task"
   └─ "30 days after questionnaire → Send new questionnaire"
   └─ Background job runs these periodically

✅ EventBasedTriggerService
   └─ "When Consent Signed → Advance to Enrollment"
   └─ "When form response = high risk → Create urgent task"
   └─ Fires immediately when event occurs

✅ FormResponseService
   └─ Accept form submission
   └─ Validate responses
   └─ Evaluate conditional logic
   └─ Auto-create tasks + transition stages

✅ CommunicationService
   └─ Design pattern for Email/SMS/WhatsApp/Voice
   └─ Queue messages for sending
   └─ Track delivery status
   └─ (No actual integration yet, design only)
```

### New Endpoints (4)

```
✅ GET  /api/workflow/patients/{id}/timeline
   └─ Returns ordered list of events
   └─ Filters by VisibleToPatient

✅ GET  /api/workflow/patients/{id}/tasks
   └─ Returns patient's active tasks
   └─ Sortable by DueDate, Priority

✅ POST /api/workflow/pathways/{id}/submit-form
   └─ Submit form with conditional logic
   └─ Returns "TriggeredActions" showing what auto-happened
   └─ Example: "Task created", "Email sent", "Stage advanced"

✅ GET  /api/workflow/pathways/{id}/questionnaires
   └─ List questionnaires for this program
   └─ Shows status, sent date, completion date, next scheduled
```

### Background Processing

```
✅ WorkflowBackgroundJobService
   └─ Runs every 1 minute
   └─ Processes time-based triggers
   └─ Processes pending messages
   └─ Handles retries for failures
   └─ Coordinated across tenants
```

---

## Complete Timeline Example (Phase 2)

```
T = 01/01/2025
├─ Lead Created
│  └─ WorkflowEvent (technical, not visible to patient)
│  └─ Trigger: Check "OnLeadCreated" rules
│  └─ Action: Send Consent form via Email
│
├─ CommunicationMessage queued: Email → Patient
└─ Background job sends in 1 minute

T = 01/02/2025 02:30 PM
├─ Patient clicks Consent link + Signs
│  └─ WorkflowEvent: "ConsentSigned" (visible to patient)
│  └─ EventBasedTrigger fires: "OnConsentSigned"
│
├─ Condition: Consent signed? YES
│  └─ Action: Auto-advance to Enrollment stage
│  └─ WorkflowEvent: "StageTransitioned"
│
└─ New task created: "Insurance Verification" (for Admin)
   └─ CreationSource: "EventTriggered"
   └─ Priority: "Normal"
   └─ AssignedToRole: "Admin"

T = 01/03/2025 09:00 AM
├─ Patient officially Enrolled
│  └─ EnteredStageAt timestamp recorded
│  └─ Nurse assigned to patient
│
└─ TimeBasedTrigger starts: "7 days after enrollment"

T = 01/10/2025 (7 days later)
├─ Background job runs (every minute, checks for triggers)
│  └─ Finds pathways: enrollment_date + 7 days = today?
│  └─ TODAY = 01/10, Enrollment = 01/03, 03+7=10 ✓
│
├─ TriggerExecution record created
│  ├─ ExecutionKey: "rule-abc_pathway-def_20250110"
│  ├─ Status: "Completed"
│  └─ CreatedEntities: [task-123, message-456]
│
├─ TaskItem created: "Follow-up Call"
│  ├─ DueDate: 01/12
│  ├─ Priority: "High"
│  ├─ AssignedToRole: "Nurse"
│  └─ CreationSource: "TimeTriggered"
│
├─ CommunicationMessage created: SMS Reminder
│  ├─ Channel: "SMS"
│  ├─ Status: "Pending"
│  └─ Content: "Time for your follow-up call"
│
├─ WorkflowEvent created: "FollowUpScheduled"
│  ├─ Description: "7-day follow-up triggered"
│  └─ VisibleToPatient: true
│
└─ Background job sends SMS in 1 minute

T = 01/10/2025 03:00 PM
├─ NURSE completes phone call with patient
│  └─ TaskItem.Status = "Completed"
│  └─ TaskItem.CompletedAt = now
│  └─ TaskItem.Outcome = "Completed"
│
├─ WorkflowEvent: "TaskCompleted"
│  └─ EventBasedTrigger checks: Any rules for "OnTaskCompleted"?
│
└─ If high-risk detected in call:
   ├─ NEW urgent task created for Doctor
   ├─ Priority: "High"
   └─ WorkflowEvent: "UrgentTaskCreated" (visible to patient)

T = 01/20/2025
├─ TimeBasedTrigger: "30 days after last questionnaire"
│  └─ OR manually scheduled questionnaire
│
├─ Questionnaire sent: "Health Assessment"
│  └─ QuestionnaireResponse.Status = "Sent"
│  └─ QuestionnaireResponse.SentAt = now
│  └─ CommunicationMessage created + sent
│
└─ Timer starts: 7-day timeout to complete

T = 01/21/2025
├─ PATIENT submits questionnaire
│  ├─ Responses: {"severity": "high", "symptoms": "..."}
│  └─ QuestionnaireResponse.CompletedAt = now
│
├─ FormResponseService evaluates conditions
│  └─ Condition: "If severity = high → Create urgent doctor task"
│  └─ Condition matched!
│
├─ Auto-actions execute:
│  ├─ TASK created: "Doctor review needed" (Priority: High)
│  ├─ EMAIL sent: "We received your response, doctor will review"
│  ├─ QUESTIONNAIRE scheduled: 30 days from now
│  └─ WorkflowEvent: "QuestionnaireCompleted" + "UrgentTaskCreated"
│
└─ API Response shows:
   {
	 "success": true,
	 "events": [
	   "Questionnaire submitted",
	   "Urgent task created for Doctor",
	   "Email sent: Follow-up notification",
	   "Next questionnaire scheduled for 02/20"
	 ]
   }

[TIMELINE VISIBLE TO PATIENT AT ANY TIME]
GET /api/workflow/patients/{id}/timeline
Response shows all events they should see:
- Consent Signed
- Patient Enrolled
- Follow-up Scheduled
- Follow-up Completed
- Questionnaire Sent
- Questionnaire Completed
- Urgent Review Needed
(NOT shown: technical events like "TriggerExecution")
```

---

## Requirement Coverage Matrix

| Requirement | Phase 1 | Phase 2 | Implementation |
|---|---|---|---|
| **Timeline visible to patient** | ❌ | ✅ | WorkflowEvent + API |
| **Event tracking** | ⚠️ Basic | ✅ Full | Every action recorded with timestamp |
| **Task management** | ⚠️ Minimal | ✅ Complete | All 10 properties: due date, priority, assigned, outcome |
| **Time-based triggers** | ❌ | ✅ | "N days after event" processing |
| **Event-based triggers** | ⚠️ Simple | ✅ Full | Rules fire on form submit, task complete, etc. |
| **Retry handling** | ❌ | ✅ | TriggerExecution tracks attempts + backoff |
| **Duplicate prevention** | ❌ | ✅ | ExecutionKey ensures once-per-day |
| **Failure tracking** | ❌ | ✅ | LastError + NextRetryAt |
| **Form conditional logic** | ⚠️ Store | ✅ Full | Response → Action (task, stage, message) |
| **Questionnaire scheduling** | ❌ | ✅ | RecurrencePattern + Timeout |
| **Questionnaire response tracking** | ❌ | ✅ | Status: Sent/Opened/Completed |
| **Communication design** | ❌ | ✅ | Provider pattern (SMS, Email, WhatsApp, Voice ready) |
| **Auto-task creation** | ⚠️ Basic | ✅ Full | From form, questionnaire, trigger |
| **Auto-stage transition** | ⚠️ Basic | ✅ Enhanced | Conditional progression |
| **Audit trail** | ✅ Partial | ✅ Complete | Every event + trigger + message logged |

---

## How to Use These Documents

### For Understanding Requirements
1. Read this file (you are here)
2. Read EXTENDED_REQUIREMENTS_IMPLEMENTATION.md (complete specifications)
3. Refer to complete timeline example above

### For Planning Implementation
1. Review PHASE_2_IMPLEMENTATION_ROADMAP.md (4-week schedule)
2. See success metrics + budget estimate
3. Understand weekly milestones

### For Implementing Code
1. Follow PHASE_2_QUICK_START.md (step-by-step)
2. Copy entity code
3. Create migrations
4. Implement services
5. Test

---

## Key Innovations in Phase 2 Design

### 1. **Event Sourcing Principle**
Every action becomes an immutable event. Timeline is audit trail.

### 2. **Trigger Execution Separation**
Separate TriggerExecution entity tracks:
- When trigger SHOULD run (scheduled time)
- When trigger DID run (executed time)
- If it failed, retry status
- Unique key to prevent duplicates

### 3. **Provider Pattern for Communication**
Design abstraction for Email/SMS/WhatsApp/Voice:
```csharp
public interface ICommunicationProvider
{
	Task<Result> SendAsync(Message message);
}
```
Can integrate any provider later without code changes.

### 4. **Visibility Control**
VisibleToPatient flag on WorkflowEvent:
- Patient sees: "Enrollment completed", "Task due", "Questionnaire sent"
- Patient doesn't see: "TriggerExecuted", "RuleEvaluated" (technical)
- Staff sees everything (audit)

### 5. **Conditional Progression**
Form responses trigger complex logic:
```json
{
  "condition": {"field": "severity", "equals": "high"},
  "actions": [
	{"type": "CreateTask", "for": "Doctor"},
	{"type": "SendMessage", "channel": "Email"},
	{"type": "TransitionStage", "to": "Urgent"}
  ]
}
```

---

## Questions Answered

### Q: "How do you prevent duplicate triggers?"
**A:** ExecutionKey = $"rule-{id}_pathway-{id}_{date}". Unique per rule/patient/day. Check before executing.

### Q: "What if a trigger fails?"
**A:** TriggerExecution tracks: LastError, AttemptCount, NextRetryAt. Retry up to MaxRetries with exponential backoff.

### Q: "Can form responses auto-create tasks?"
**A:** Yes! FormResponseService evaluates conditions and executes actions. "If severity=high → Create urgent task"

### Q: "How long until a questionnaire times out?"
**A:** Questionnaire.TimeoutDays specifies. Status goes to "Abandoned" if not completed by then.

### Q: "Can we add SMS later?"
**A:** Absolutely! CommunicationService uses provider pattern. Add SmsCommunicationProvider whenever ready.

---

## Implementation Timeline

```
Phase 1: COMPLETE ✅
└─ Configuration layer working
└─ Patient enrollment working
└─ Basic form submission working

Phase 2: READY TO BUILD 🚀
├─ Week 1: Add 6 new entities + migration (1 day)
├─ Week 2: Implement trigger services (3-4 days)
├─ Week 3: Create controllers + endpoints (2 days)
├─ Week 4: Background job + integration tests (2 days)
└─ Total: ~10 days

Result: Production-ready event-driven healthcare workflow
```

---

## Success Criteria (What You Get)

✅ **Complete Timeline**
- Patient sees their journey chronologically
- Every event timestamped and described
- Non-technical events hidden from patient

✅ **Automatic Workflows**
- Time-based: "7 days after enrollment → follow-up task"
- Event-based: "Form submitted → create task"
- Conditional: "High risk response → urgent task + email"

✅ **Task Management**
- Full task properties (due date, priority, assigned, outcome)
- Created manually or automatically
- Completion tracking + notes

✅ **Questionnaire Management**
- Send on schedule (one-time or recurring)
- Track: sent, opened, completed
- Auto-trigger next actions on response

✅ **Communication Ready**
- Abstract design for Email/SMS/WhatsApp/Voice
- Queue messages + track delivery
- Can integrate true providers incrementally

✅ **Reliability**
- Retry failed triggers
- Prevent duplicate execution
- Full audit trail of all activity

---

## Bottom Line

**Phase 2 fully implements the extended requirements** by adding:
- 6 new entities
- 4 services with complex logic
- 4 new API endpoints
- 1 background job service
- Complete retry/duplicate/failure handling
- Communication system (design pattern)

**All documented with:**
- Implementation guide (EXTENDED_REQUIREMENTS_IMPLEMENTATION.md)
- Roadmap (PHASE_2_IMPLEMENTATION_ROADMAP.md)
- Quick start (PHASE_2_QUICK_START.md)

**Ready to build? Start with PHASE_2_QUICK_START.md Step 1.** 🚀
