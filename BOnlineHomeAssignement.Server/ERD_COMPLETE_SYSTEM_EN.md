# Entity Relationship Diagram (ERD): Care Platform System
## Complete Database Schema Design

---

## Complete System ERD

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                                 MULTI-TENANT CORE                                │
└──────────────────────────────────────────────────────────────────────────────────┘

									┌─────────────┐
									│   TENANT    │
									│─────────────│
									│ • TenantId  │
									│ • Name      │
									│ • Logo      │
									│ • TimeZone  │
									└─────────────┘
										  │
					┌─────────────────────┼─────────────────────┐
					│                     │                     │
					▼                     ▼                     ▼
		┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
		│     PATIENT      │  │     PROGRAM      │  │  TENANT CONFIG   │
		│──────────────────│  │──────────────────│  │──────────────────│
		│ • PatientId      │  │ • ProgramId      │  │ • TenantConfigId │
		│ • TenantId (FK)  │  │ • Name           │  │ • TenantId (FK)  │
		│ • FirstName      │  │ • Description    │  │ • MaxPatients    │
		│ • LastName       │  │ • StartDate      │  │ • MaxPrograms    │
		│ • Email          │  │ • EndDate        │  │ • Features       │
		│ • Phone          │  │ • IsActive       │  │ • Branding       │
		│ • DateOfBirth    │  └──────────────────┘  └──────────────────┘
		│ • PrimaryDocId   │           │
		│ • AssignedNurseId│           │
		│ • CreatedAt      │           ▼
		└────────┬─────────┘  ┌──────────────────────────────┐
				 │            │ PROGRAM CONFIGURATION        │
				 │            │──────────────────────────────│
				 │            │ • ConfigId                   │
				 │            │ • TenantId (FK)              │
				 │            │ • ProgramId (FK)             │
				 │            │ • WorkflowType               │
				 │            │ • WorkflowDefinition (JSON)  │
				 │            │ • DefaultBusinessRules (JSON)│
				 │            └────────┬─────────────────────┘
				 │                     │
				 │         ┌───────────┼───────────┐
				 │         │           │           │
				 │         ▼           ▼           ▼
				 │    ┌──────────┐ ┌──────────┐ ┌──────────────┐
				 │    │PROCESS   │ │FORM      │ │BUSINESS RULE │
				 │    │STAGE     │ │DEFINITION│ │──────────────│
				 │    │──────────│ │──────────│ │ • RuleId     │
				 │    │ StageId  │ │FormId    │ │ • TenantId   │
				 │    │ Name     │ │TenantId  │ │ • Name       │
				 │    │ Order    │ │ProgramId │ │ • Condition  │
				 │    │ Duration │ │Title     │ │ • Action     │
				 │    │ Status   │ │Fields    │ │ • Priority   │
				 │    └──────────┘ │Required  │ │ • IsActive   │
				 │         │       │OnSubmit  │ └──────┬───────┘
				 │         │       └──────────┘        │
				 │         │                           │
				 └─────────┼─────────────────────────────┴──────────┐
						   │                                         │
						   ▼                                         ▼
				┌────────────────────────────────────────────────────────────┐
				│               PATIENT PATHWAY                              │
				│  (Patient's complete journey through a program)            │
				│────────────────────────────────────────────────────────────│
				│ • PathwayId                                                │
				│ • TenantId (FK)                                            │
				│ • PatientId (FK) ────────────────┐                         │
				│ • EnrollmentId (FK)              │ (Links back to Patient) │
				│ • ProgramConfigId (FK)           │                         │
				│ • CurrentProcessStageId (FK) ────────────┐                 │
				│ • CurrentStatus                   (FK: ProcessStage)       │
				│ • PatientType                            │                 │
				│ • PathwayData (JSON)                     │                 │
				│ • EnteredStageAt                         ▼                 │
				│ • StageDueAt                      ┌──────────────┐         │
				│ • CompletedAt                     │PROCESS STAGE │         │
				│ • IsActive                        │──────────────│         │
				└────────────────────────────────────│ • StageId    │─────────┘
													│ • Name       │
													│ • Order      │
													│ • Duration   │
													│ • Status     │
													│ • AutoAdvance│
													└──────────────┘

┌──────────────────────────────────────────────────────────────────────────────────┐
│                            WORKFLOW TIMELINE & EVENTS                            │
└──────────────────────────────────────────────────────────────────────────────────┘

		┌────────────────────────────────────────────────────────┐
		│            WORKFLOW EVENT (Timeline)                   │
		│────────────────────────────────────────────────────────│
		│ • EventId                                              │
		│ • TenantId (FK)                                        │
		│ • PathwayId (FK) ─────────────┐                        │
		│ • EventType                   │                        │
		│ • Description                 ├─────────────────────┐  │
		│ • TriggeredBy                 │                     │  │
		│ • EventData (JSON)            │                     │  │
		│ • VisibleToPatient            │                     │  │
		│ • EventOccurredAt             │                     │  │
		│ • CreatedAt                   │                     │  │
		└────────────────────────────────────────────────────────┘
												│
												│ (Links to Pathway)
												│
		┌────────────────────────────────┐     │
		│   QUESTIONNAIRE                │     │
		│────────────────────────────────│     │
		│ • QuestionnaireId              │     │
		│ • TenantId (FK)                │     │
		│ • ProgramConfigId (FK)         │     │
		│ • Title                        │     │
		│ • FieldsDefinition (JSON)      │     │
		│ • RecurrenceType               │     │
		│ • RecurrenceValue              │     │
		│ • MaxSendCount                 │     │
		│ • IsActive                     │     │
		└─────────┬────────────────────────────┘
				  │
				  ▼
		┌──────────────────────────────┐
		│ QUESTIONNAIRE RESPONSE       │
		│──────────────────────────────│
		│ • ResponseId                 │
		│ • TenantId (FK)              │
		│ • QuestionnaireId (FK)       │
		│ • PathwayId (FK) ────────────┼──→ (Links back to Pathway)
		│ • ResponseData (JSON)        │
		│ • SentAt                     │
		│ • CompletedAt                │
		│ • Status                     │
		└──────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────────┐
│                         ENROLLMENT & TRACKING                                    │
└──────────────────────────────────────────────────────────────────────────────────┘

		┌──────────────────────────┐
		│    ENROLLMENT            │
		│──────────────────────────│
		│ • EnrollmentId           │
		│ • PatientId (FK) ───────────┐
		│ • ProgramId (FK)         │   │
		│ • TenantId (inherited)   │   │
		│ • EnrolledAt             │   │
		│ • Status                 │   │
		└──────────────────────────┘   │
									   ▼ (Links to Patient)
		┌──────────────────────────┐
		│  TASK ITEM               │
		│──────────────────────────│
		│ • TaskId                 │
		│ • TenantId (FK)          │
		│ • PathwayId (FK)         │
		│ • Title                  │
		│ • Description            │
		│ • AssignedTo             │
		│ • Status                 │
		│ • DueDate                │
		│ • CompletedAt            │
		└──────────────────────────┘

		┌────────────────────────────────────────────┐
		│  TRIGGER EXECUTION                         │
		│────────────────────────────────────────────│
		│ • ExecutionId                              │
		│ • TenantId (FK)                            │
		│ • PathwayId (FK)                           │
		│ • TriggerType                              │
		│ • Status (Pending/Processing/Succeeded)    │
		│ • AttemptCount                             │
		│ • ExecutedAt                               │
		│ • ErrorMessage                             │
		│ • CreatedAt                                │
		└────────────────────────────────────────────┘

		┌────────────────────────────────────────────┐
		│  ROLE PERMISSION                           │
		│────────────────────────────────────────────│
		│ • PermissionId                             │
		│ • TenantId (FK)                            │
		│ • ProgramConfigId (FK)                     │
		│ • RoleName (Doctor, Nurse, etc.)           │
		│ • AccessibleStages (CSV)                   │
		│ • Permissions (Read, Edit, Approve)        │
		│ • CanApproveTransitions                    │
		│ • CanOverrideRules                         │
		└────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────────┐
│                        AUDIT & COMPLIANCE                                        │
└──────────────────────────────────────────────────────────────────────────────────┘

		┌────────────────────────────────────────────┐
		│  AUDIT LOG                                 │
		│────────────────────────────────────────────│
		│ • LogId                                    │
		│ • TenantId (FK)                            │
		│ • UserId                                   │
		│ • Action (Create/Edit/Delete/View)         │
		│ • EntityType (Patient, Program, etc.)      │
		│ • EntityId                                 │
		│ • OldValue (before)                        │
		│ • NewValue (after)                         │
		│ • Reason                                   │
		│ • IpAddress                                │
		│ • Timestamp                                │
		└────────────────────────────────────────────┘

		┌────────────────────────────────────────────┐
		│  COMMUNICATION MESSAGE                     │
		│────────────────────────────────────────────│
		│ • MessageId                                │
		│ • TenantId (FK)                            │
		│ • PathwayId (FK)                           │
		│ • Type (Email/SMS/Push)                    │
		│ • Recipient                                │
		│ • Subject                                  │
		│ • Content                                  │
		│ • Status (Queued/Sent/Failed)              │
		│ • SentAt                                   │
		│ • FailureReason                            │
		│ • RetryCount                               │
		└────────────────────────────────────────────┘
```

---

## Key Relationships Summary

| From | To | Cardinality | Notes |
|------|----|-------------|-------|
| **Tenant** | Patient | 1:N | Every patient belongs to exactly 1 tenant |
| **Tenant** | Program | 1:N | Tenant has multiple programs |
| **Patient** | Enrollment | 1:N | Patient can enroll in multiple programs |
| **Enrollment** | Program | N:1 | Many enrollments point to programs |
| **Enrollment** | PatientPathway | 1:1 | One pathway per enrollment |
| **PatientPathway** | WorkflowEvent | 1:N | Pathway has many timeline events |
| **PatientPathway** | ProcessStage | N:1 | Pathway in one current stage |
| **PatientPathway** | QuestionnaireResponse | 1:N | Pathway has multiple responses |
| **ProgramConfiguration** | ProcessStage | 1:N | Program has multiple stages |
| **ProgramConfiguration** | BusinessRule | 1:N | Program has multiple rules |
| **BusinessRule** | TriggerExecution | 1:N | Rule can execute multiple times |

---

## Data Flow Example: Patient Form Submission

```
1. Patient submits form in portal
   ↓
2. FormSubmission event published to message queue
   ↓
3. Background worker pulls event from queue
   ↓
4. Find all BusinessRules WHERE TriggerEvent = "OnFormSubmit"
   ↓
5. For each rule (ordered by Priority):
   a. Load patient pathway context
   b. Evaluate Condition (JSON rule engine)
   c. If TRUE → Execute Action
   d. Create TriggerExecution record (success/failure/error)
   ↓
6. Create WorkflowEvent (timeline entry visible to patient)
   ↓
7. If action was "StageTransition":
   a. Update PatientPathway.CurrentProcessStageId
   b. Update PatientPathway.EnteredStageAt = now
   c. Send notification to patient/care team
   d. Create follow-up tasks in TaskItem table
   e. Trigger additional events if needed
   ↓
8. Log everything to AuditLog table
   ↓
9. Message marked as processed (acknowledged)
   ↓
10. System continues to next message in queue
```

---

## Database Indexing Strategy

```sql
-- Tenant isolation (MUST be on every major query)
CREATE INDEX IX_Tenant_Entity ON Patients(TenantId, Id);
CREATE INDEX IX_Tenant_Entity ON Enrollments(TenantId, Id);
CREATE INDEX IX_Tenant_Entity ON PatientPathways(TenantId, Id);
CREATE INDEX IX_Tenant_Entity ON WorkflowEvents(TenantId, PathwayId);
CREATE INDEX IX_Tenant_Entity ON BusinessRules(TenantId, IsActive);

-- Query performance
CREATE INDEX IX_Enrollment_Patient_Program 
  ON Enrollments(PatientId, ProgramId, TenantId);
CREATE INDEX IX_Pathway_Patient 
  ON PatientPathways(PatientId, TenantId);
CREATE INDEX IX_Event_Type_Date 
  ON WorkflowEvents(TenantId, EventType, EventOccurredAt);

-- Time-based queries (for follow-ups, scheduling)
CREATE INDEX IX_Pathway_DueDate 
  ON PatientPathways(TenantId, StageDueAt, IsActive);

-- Audit and compliance
CREATE INDEX IX_AuditLog_Tenant_User 
  ON AuditLogs(TenantId, UserId, Timestamp);

-- Queue processing
CREATE INDEX IX_TriggerExecution_Status 
  ON TriggerExecutions(TenantId, Status, CreatedAt);
```

---

## Design Principles

✅ **TenantId on every table** - enforces isolation at storage layer  
✅ **Soft deletes** - IsActive flag instead of hard deletes (for audit trails)  
✅ **CreatedAt / UpdatedAt** - timestamp tracking for compliance  
✅ **Event sourcing ready** - WorkflowEvent captures all changes  
✅ **Immutable workflows** - once event recorded, never deleted (audit trail)  
✅ **Foreign key constraints** - referential integrity enforced  
✅ **JSON columns** - for flexible rule/workflow definitions  
✅ **Composite keys with TenantId** - ensures multi-tenant safety  

---

## Entity Dependencies

### Core Patient Journey
```
Tenant
  ↓
Patient (TenantId required)
  ↓
Enrollment (PatientId + ProgramId)
  ↓
PatientPathway (EnrollmentId + ProgramConfigId)
  ↓
ProcessStage (current stage of pathway)
  ↓
WorkflowEvent (timeline of pathway)
```

### Configuration & Rules
```
Tenant
  ↓
Program
  ↓
ProgramConfiguration (TenantId + ProgramId)
  ↓
ProcessStage (stages in workflow)
BusinessRule (rules engine)
FormDefinition (forms at stages)
TaskDefinition (tasks to create)
RolePermission (who can do what)
```

### Automation & Execution
```
BusinessRule
  ↓
TriggerExecution (when rule ran, success/failure)
  ↓
WorkflowEvent (result logged to timeline)
  ↓
TaskItem (tasks created if needed)
CommunicationMessage (notifications if needed)
```

---

## Migration Path (from legacy system)

```sql
-- Phase 1: Add TenantId to existing tables
ALTER TABLE Patients ADD TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
ALTER TABLE Programs ADD TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();

-- Phase 2: Map legacy data to tenant
UPDATE Patients SET TenantId = @defaultTenantId WHERE TenantId = ...;

-- Phase 3: Create constraints
ALTER TABLE Patients ADD CONSTRAINT FK_Patient_Tenant 
  FOREIGN KEY (TenantId) REFERENCES Tenants(Id);

-- Phase 4: Add indexes
CREATE INDEX IX_Tenant_Patient ON Patients(TenantId, Id);
```

---

**Schema Ready for Implementation | Next: SQL DDL Script**
