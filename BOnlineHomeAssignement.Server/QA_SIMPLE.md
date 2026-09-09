# Simple Q&A: System Architecture

## Q1: Can a patient participate in multiple programs?
**A:** Yes. Each patient can enroll in multiple programs through separate **Enrollment** records.

**Example:**
- Patient "John" → Enrollment #1 → Hypertension Program
- Patient "John" → Enrollment #2 → Weight Loss Program
- Patient "John" → Enrollment #3 → Cardiac Program

---

## Q2: Can the same patient appear in multiple tenants?
**A:** No. Each patient has a required `TenantId` field, so they belong to exactly one tenant.

**Why?** For data isolation and security.

---

## Q3: Who owns the Timeline?
**A:** **PatientPathway** owns the timeline.

**How it works:**
- Patient → Enrollment → **PatientPathway** (tracks patient's journey in a program)
- PatientPathway → WorkflowEvents (timeline entries)

**Each enrollment has its own timeline.**

---

## Q4: Can a nurse belong to multiple programs?
**A:** Yes. Through **RolePermission** records.

**Example:**
- Nurse "Lisa" → RolePermission #1 → Program A (permissions: Read, Edit)
- Nurse "Lisa" → RolePermission #2 → Program B (permissions: Read only)
- Nurse "Lisa" → RolePermission #3 → Program C (permissions: Read, Edit, Approve)

---

## Q5: Does each tenant use a shared or separate database?
**A:** **Shared database** with tenant isolation.

**How:**
- Single database
- Every table has `TenantId` column
- All queries must filter by `TenantId`

**Example:**
```
Query: SELECT * FROM Patients WHERE TenantId = @tenantId
(Not filtering = data leak!)
```

---

## Q6: How are workflow rules stored and executed?
**A:** Through **BusinessRule** and **TriggerExecution** tables.

**Storage:**
- **BusinessRule** table stores rules with:
  - `Condition` (JSON: "if patient type = Chronic AND score > 80")
  - `Action` (JSON: "then transition to Stage 2")
  - `TriggerEvent` ("OnFormSubmit", "OnStageEntry", etc.)

**Execution:**
- When an event occurs (form submitted), system:
  1. Finds matching BusinessRules
  2. Evaluates conditions
  3. Executes actions (transitions, notifications, tasks)
  4. Records execution in TriggerExecution table
  5. Creates WorkflowEvent for timeline

**Example Rule:**
- **Name:** "Auto-advance chronic patients"
- **Trigger:** OnFormSubmit
- **Condition:** patientType = "Chronic" AND formScore > 80
- **Action:** Move to "Monitoring" stage
- **Result:** Rule fires automatically when conditions met

---

## Quick Reference Table

| Question | Answer |
|----------|--------|
| Multiple programs per patient? | ✅ Yes (via Enrollment) |
| Multiple tenants per patient? | ❌ No (TenantId required) |
| Timeline owner? | PatientPathway |
| Nurse in multiple programs? | ✅ Yes (via RolePermission) |
| Database type? | Shared (TenantId filtering) |
| Workflow rules? | BusinessRule + TriggerExecution |

---

## Key Entities

```
Patient
  ├── TenantId (required) - belongs to one tenant
  └── Enrollments (multiple)
	  └── Program
	  └── PatientPathway
		  ├── ProcessStage
		  └── WorkflowEvents (Timeline)

BusinessRule
  ├── Condition (JSON)
  ├── Action (JSON)
  ├── TriggerEvent
  └── TriggerExecution (tracks if rule ran)

RolePermission
  ├── Nurse/Staff
  ├── Program
  └── Permissions (Read, Write, Approve, etc.)
```

---

**Ready to answer your examiner!**
