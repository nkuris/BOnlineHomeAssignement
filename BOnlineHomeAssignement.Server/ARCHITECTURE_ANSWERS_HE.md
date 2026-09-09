# תשובה מקיפה: מימוש מערכת ניהול patient care

**נושא:** מימוש אדריכלות multi-tenant עם workflow ניתנים להתאמה  
**תאריך:** ספטמבר 2026  
**שפה:** עברית

---

## 📋 תוכן עניינים

1. [יכולת השתתפות Patient במספר Programs](#1-יכולת-השתתפות-patient-במספר-programs)
2. [האם אותו אדם יכול להופיע אצל יותר מ-Tenant אחד](#2-האם-אותו-אדם-יכול-להופיע-אצל-יותר-מ-tenant-אחד)
3. [שייכות Timeline](#3-שייכות-timeline)
4. [אחות בין-Programs](#4-האם-אחות-יכולה-להשתייך-למספר-programs)
5. [Tenant וDatabase](#5-האם-tenant-משתמש-ב-db-משותף-או-נפרד)
6. [שמירה והפעלת Workflow Rules](#6-איך-נשמרים-ומופעלים-workflow-rules)

---

## 1. יכולת השתתפות Patient במספר Programs

### ✅ **תשובה: כן, ניתן לכך מימוש מלא**

#### המימוש:
- **Patient** (Patient.cs) - מייצג חולה יחיד
- **Program** (Program.cs) - מייצג תוכנית טיפול
- **Enrollment** (Enrollment.cs) - מייצג התחברות (חיבור) בין Patient ל-Program

#### מבנה הקשרים (Relationships):
```csharp
public class Patient
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }  // ← כל patient שייך ל-Tenant אחד בלבד
	public string FirstName { get; set; }
	public string LastName { get; set; }
	// ...
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

#### דוגמה עבודה:
```
Patient: "רונן כהן" (ID: abc-123)
├── Enrollment #1 → Program: "תוכנית טיפול בלחץ דם"
├── Enrollment #2 → Program: "תוכנית ירידה במשקל"
└── Enrollment #3 → Program: "תוכנית שיקום לבבי"
```

#### יתרון:
- **Flexibility** - חולה יכול להיות בכמה תוכניות בו זמנית
- **Isolation** - כל תוכנית יש את ה-workflow שלה עצמאית
- **Tracking** - מעקב נפרד לכל enrollment

---

## 2. האם אותו אדם יכול להופיע אצל יותר מ-Tenant אחד

### ❌ **תשובה: לא, בעיצוב הנוכחי אי אפשר**

#### הסיבה:
```csharp
public class Patient
{
	[Required]
	public Guid TenantId { get; set; }  // ← Required! לא ניתן להשמיט
}
```

- **TenantId** הוא **Required** (חובה)
- כל Patient שייך **בדיוק ל-Tenant אחד**
- זו בחירה עיצובית כדי להבטיח **data isolation**

#### אם נרצה לשנות זאת:
נצטרך ליצור טבלה ביניים (junction table):
```csharp
public class PatientTenantMapping
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }
	public Guid TenantId { get; set; }
	public DateTime AssignedAt { get; set; }
}
```

#### החלטה עיצובית:
**בחרנו בדרך הראשונה (Tenant אחד לפי Patient)** מכיוון שזה:
- ✅ פשוט יותר
- ✅ בטוח יותר (data isolation מובנה)
- ✅ מתאים לרוב המקרים (כל טיפול עם Tenant אחד)

---

## 3. שייכות Timeline

### ✅ **תשובה: Timeline שייך ל-PatientPathway**

#### המבנה:
```csharp
public class PatientPathway
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }
	public Guid EnrollmentId { get; set; }
	public Guid ProgramConfigurationId { get; set; }

	// ← Timeline הוא כל ה-workflow עבור enrollment זה
	public DateTime EnteredStageAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	// ...
}

public class WorkflowEvent
{
	public Guid Id { get; set; }

	[Required]
	public Guid PatientPathwayId { get; set; }  // ← קשר ישיר ל-PatientPathway
	public PatientPathway? PatientPathway { get; set; }

	public string EventType { get; set; }  // "Enrolled", "StageTransitioned", etc.
	public string Description { get; set; }
	public DateTime EventOccurredAt { get; set; }
	public bool VisibleToPatient { get; set; }
}
```

#### דוגמה היררכיה:
```
Patient: "שרה ישראלי"
└── Enrollment: "Program: ניהול סוכרת"
	└── PatientPathway #1
		├── Stage 1: "הערכה ראשונית"
		│   └── WorkflowEvent: "שרה הגיעה ל-Stage 1" (2026-09-01)
		├── Stage 2: "תכנון טיפול"
		│   └── WorkflowEvent: "שרה עברה ל-Stage 2" (2026-09-15)
		└── Stage 3: "ניטור"
			└── WorkflowEvent: "שרה עברה ל-Stage 3" (2026-10-01)
```

#### מה שנשמר ב-Timeline:
- **קבוצת events** ל-PatientPathway
- **כל event** קשור ישירות ל-PatientPathway
- **כל Enrollment שונה** = PatientPathway שונה = Timeline שונה

---

## 4. האם אחות יכולה להשתייך למספר Programs

### ✅ **תשובה: כן, דרך RolePermissions**

#### המבנה:
```csharp
public class RolePermission
{
	public Guid Id { get; set; }

	[Required]
	public Guid TenantId { get; set; }

	[Required]
	public Guid ProgramConfigurationId { get; set; }  // ← RolePermission שונה לכל Program

	[Required]
	public string RoleName { get; set; }  // "Nurse", "Doctor", "CareCoordinator"

	public string? AccessibleStages { get; set; }  // "Stage1,Stage2,Stage3"
	public string? Permissions { get; set; }       // "Read,Create,Edit,Approve"
	public string? AssignableTaskTypes { get; set; }  // "Assessment,FollowUp"

	public bool CanViewPathways { get; set; }
	public bool CanApproveTransitions { get; set; }
	public bool CanOverrideRules { get; set; }
}
```

#### דוגמה:
```
Nurse: "ליאור כהן"
├── RolePermission #1
│   └── Program: "ניהול סוכרת"
│       └── Permissions: "Read,Create,Edit"
│       └── AccessibleStages: "Stage1,Stage2"
│
└── RolePermission #2
	└── Program: "שיקום לבבי"
		└── Permissions: "Read,Edit"
		└── AccessibleStages: "Stage2,Stage3"
```

#### היתרונות:
- ✅ **גמיש** - אחות יכולה להיות ב-Programs רבים
- ✅ **בטוח** - כל Program יכול להגדיר permissions שונים
- ✅ **מדויק** - ניקוד על בסיס Role + Program

---

## 5. האם Tenant משתמש ב-DB משותף או נפרד

### 🔷 **תשובה: DB משותף עם Data Isolation**

#### המימוש:
```csharp
public class Patient
{
	[Required]
	public Guid TenantId { get; set; }  // ← קיים בכל טבלה
}

public class Enrollment
{
	[Required]
	public Guid PatternId { get; set; }  // ← לא קיים! היא inherit מ-Patent דרך relationship
}
```

#### פילטור טנציה (Tenant Filtering):
כל query צריך לסנן לפי TenantId הנוכחי:
```csharp
// ❌ לא בטוח:
var patients = await context.Patients.ToListAsync();

// ✅ בטוח:
var patients = await context.Patients
	.Where(p => p.TenantId == currentTenantId)
	.ToListAsync();
```

#### יתרונות:
- ✅ **Cost effective** - DB אחד לכל ה-Tenants
- ✅ **Maintenance קל** - migration אחד לכולם
- ✅ **Scalability** - קל להוסיף Tenants חדשים

#### חסרונות:
- ❌ צריך להיות זהיר בפילטור
- ❌ בגים ב-query יכולים לחשוף data של Tenant אחר

#### דרך מומלצת (Multitenancy Pattern):
```csharp
public class TenantMiddleware
{
	// שומרת את TenantId הנוכחי ב-DbContext
	public void SetTenantId(Guid tenantId)
	{
		_context.TenantId = tenantId;
	}
}

// בכל query:
var patients = await _context.Patients
	.Where(p => p.TenantId == _context.TenantId)  // ← אוטומטי
	.ToListAsync();
```

---

## 6. איך נשמרים ומופעלים Workflow Rules

### ✅ **תשובה: דרך BusinessRule entity ו-TriggerExecution**

#### A. שמירת Rules:

```csharp
public class BusinessRule
{
	public Guid Id { get; set; }

	[Required]
	public Guid ProgramConfigurationId { get; set; }

	[Required]
	public string Name { get; set; }  // "Auto-advance to Final Stage"

	public string RuleType { get; set; }  // "StageTransition", "TaskTrigger", "Notification"

	public string TriggerEvent { get; set; }  // "OnFormSubmit", "OnStageEntry", "Daily"

	// ← הכלל כ-JSON
	[Required]
	public string Condition { get; set; }  
	// Example: 
	// {
	//   "type": "and",
	//   "conditions": [
	//     { "field": "patientType", "operator": "equals", "value": "Chronic" },
	//     { "field": "formScore", "operator": "greaterThan", "value": 70 }
	//   ]
	// }

	[Required]
	public string Action { get; set; }
	// Example:
	// {
	//   "type": "transition",
	//   "targetStageId": "stage-uuid-123",
	//   "notification": "Patient advanced automatically"
	// }

	public int Priority { get; set; }  // 1 = highest priority
	public bool IsActive { get; set; }
}
```

#### B. הפעלת Rules דרך Triggers:

```csharp
public class TriggerExecution
{
	public Guid Id { get; set; }

	[Required]
	public Guid PatientPathwayId { get; set; }  // ← אני חוקק אצל patient זה

	public string TriggerType { get; set; }  
	// "TimeBasedFollowUp", "EventBasedFormResponse", "QuestionnaireSchedule"

	[MaxLength(50)]
	public string Status { get; set; }  // "Pending", "Processing", "Succeeded", "Failed"

	public int AttemptCount { get; set; }
	public DateTime? ExecutedAt { get; set; }
	public string? ErrorMessage { get; set; }
}
```

#### C. Flow של הפעלה:

```
1️⃣ Patient עבר ל-ProcessStage חדש
   └── "OnStageEntry" event מופעל

2️⃣ System חוקר את כל BusinessRules עם TriggerEvent="OnStageEntry"
   └── מוצא: "Rule: Check if patient is Chronic"

3️⃣ System בודק את ה-Condition
   └── אם TRUE: בצע את ה-Action

4️⃣ Action מבוצע (למשל: transition, send notification, create task)
   └── TriggerExecution מעודכן עם Status="Succeeded"

5️⃣ WorkflowEvent נוצר (Timeline update)
   └── "Trigger: Auto-advance to Final Stage" מופיע בTimeline
```

#### D. דוגמה מלאה:

```csharp
// Rule: כל patient chronic עם score > 70 מעבור לשלב סיום
var rule = new BusinessRule
{
	Name = "Auto-advance Chronic Patients",
	RuleType = "StageTransition",
	TriggerEvent = "OnFormSubmit",
	Condition = JsonConvert.SerializeObject(new {
		type = "and",
		conditions = new[] {
			new { field = "patientType", op = "eq", value = "Chronic" },
			new { field = "formScore", op = "gt", value = 70 }
		}
	}),
	Action = JsonConvert.SerializeObject(new {
		type = "transition",
		targetStageId = finalStageId,
		message = "Automatically advanced based on score"
	}),
	Priority = 1,
	IsActive = true
};

await context.BusinessRules.AddAsync(rule);

// כשנתונים מוגשים:
var execution = new TriggerExecution
{
	PatientPathwayId = patientPathwayId,
	TriggerType = "EventBasedFormResponse",
	Status = "Pending"
};

await context.TriggerExecutions.AddAsync(execution);
// ← System יבצע את הבדיקה וההעברה אוטומטית
```

---

## 📊 דיאגרמה של הקשרים

```
┌─────────────────────────────────────────────────────────────┐
│                        TENANT                               │
│  (רמת ארגון - בתי חולים, משרדים, וכו')                       │
└─────────────────────────────────────────────────────────────┘
							  │
			┌─────────────────┼─────────────────┐
			│                 │                 │
	┌───────▼────────┐ ┌─────▼────────┐ ┌──────▼──────┐
	│   PATIENT #1   │ │ PATIENT #2   │ │ PATIENT #3  │
	│  (רונן כהן)    │ │ (שרה ישראלי) │ │ (דוד רווה)  │
	└─────┬──────────┘ └──────────────┘ └─────────────┘
		  │
	┌─────┴──────────┬──────────────┐
	│                │              │
┌───▼──────────┐ ┌──▼──────────┐ ┌──▼──────────┐
│ ENROLLMENT 1 │ │ ENROLLMENT 2│ │ ENROLLMENT 3│
│  Program: A  │ │  Program: B │ │  Program: C │
└───┬──────────┘ └──────────────┘ └─────────────┘
	│
┌───▼──────────────────────────┐
│   PATIENT PATHWAY            │
│ (מסלול ה-patient בתוכנית A)  │
├──────────────────────────────┤
│ Stage 1: Assessment          │
│ Stage 2: Treatment           │
│ Stage 3: Monitoring          │
└──────────────────────────────┘
	│
	├─ WORKFLOW EVENTS (Timeline)
	│  ├─ "Enrolled in Program A"
	│  ├─ "Moved to Treatment Stage"
	│  └─ "Form Submitted"
	│
	├─ QUESTIONNAIRES
	│  └─ "Weekly Health Check"
	│
	├─ BUSINESS RULES
	│  └─ "If score > 70, auto-advance"
	│
	└─ TRIGGER EXECUTIONS
	   └─ "Rule evaluated at 10:00 AM"
```

---

## 🎯 סיכום עיקרי

| שאלה | תשובה | מימוש |
|------|-------|--------|
| Patient → Programs מרובים? | ✅ כן | דרך Enrollment |
| patient → Tenants מרובים? | ❌ לא | TenantId Required |
| Timeline שייכות? | PatientPathway | דרך WorkflowEvent |
| Nurse → Programs מרובים? | ✅ כן | דרך RolePermission |
| DB משותף? | ✅ כן | עם TenantId filtering |
| Workflow Rules? | ✅ כן | BusinessRule + TriggerExecution |

---

## 🔐 טיפולים אבטחוניים

```csharp
// ✅ כל query מסננת לפי TenantId:
var data = await context.Patients
	.Where(p => p.TenantId == currentTenantId)
	.ToListAsync();

// ✅ Role-Based Access Control:
if (!user.RolePermissions.Contains(permission)) 
	throw new UnauthorizedAccessException();

// ✅ Audit Trail:
foreach (var logEntry in context.AuditLogs)
	log.Info($"User:{user.Id} Action:{logEntry.Action} Entity:{logEntry.EntityName}");
```

---

## 📚 מקורות בקוד

- `BOnlineHomeAssignement.Server/Domain/Entities/Patient.cs` - Patient model
- `BOnlineHomeAssignement.Server/Domain/Entities/Enrollment.cs` - Enrollment model
- `BOnlineHomeAssignement.Server/Domain/Entities/WorkflowEntities.cs` - Workflow, Timeline, Rules
- `BOnlineHomeAssignement.Server/Infrastructure/Persistence/AppDbContext.cs` - Database mapping

---

**עמוד זה כתוב כתשובה מקיפה לבוחן על אדריכלות המערכת.**
