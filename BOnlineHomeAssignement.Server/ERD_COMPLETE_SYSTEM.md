# Entity Relationship Diagram (ERD): Care Platform System

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

## Data Flow Example: Patient Submission

```
1. Patient submits form
   ↓
2. FormSubmission event published to queue
   ↓
3. Worker pulls from queue
   ↓
4. Find all BusinessRules WHERE TriggerEvent = "OnFormSubmit"
   ↓
5. For each rule:
   a. Evaluate Condition
   b. If TRUE → Execute Action
   c. Create TriggerExecution record (success/failure)
   ↓
6. Create WorkflowEvent (timeline entry)
   ↓
7. If action was "StageTransition":
   a. Update PatientPathway.CurrentProcessStageId
   b. Send notification to patient/care team
   c. Create follow-up tasks
   ↓
8. Log everything to AuditLog
   ↓
9. Message processed successfully
```

---

## Database Indexing Strategy

```sql
-- Tenant isolation (MUST be on every major query)
CREATE INDEX IX_Tenant_Entity ON Patients(TenantId, Id);
CREATE INDEX IX_Tenant_Entity ON Enrollments(TenantId, Id);
CREATE INDEX IX_Tenant_Entity ON PatientPathways(TenantId, Id);
CREATE INDEX IX_Tenant_Entity ON WorkflowEvents(TenantId, PathwayId);

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
```

---

## Design Principles

✅ **TenantId on every table** - enforces isolation at storage layer  
✅ **Soft deletes** - IsActive flag instead of hard deletes (for audit trails)  
✅ **CreatedAt / UpdatedAt** - timestamp tracking  
✅ **Event sourcing ready** - WorkflowEvent captures all changes  
✅ **Immutable workflows** - once event recorded, never deleted  
✅ **Foreign key constraints** - referential integrity enforced  

---

**Ready for Database Schema Generation | Next: SQL Script**
