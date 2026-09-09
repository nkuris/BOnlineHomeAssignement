# Outstanding Work & Next Steps

## Current State (as of Session Completion)

✅ **Completed:**
- [x] Configurable workflow domain entities (8 entities)
- [x] WorkflowService & ConfigurationService
- [x] 36 REST API endpoints (admin + clinical)
- [x] EF Core migration
- [x] Seeded demo data
- [x] Architecture documentation (3 guides + README update)
- [x] Build succeeds (no errors/warnings)
- [x] Multi-tenant isolation pattern
- [x] Entity relationships documented

---

## Known Issues & Placeholder Code

### 1. WorkflowController.SubmitFormResponses() - Incomplete Return Flow

**Location:** `Controllers/WorkflowController.cs` → `SubmitFormResponses()` endpoint

**Issue:** The method saves form responses and triggers business rules, but the final response returns the pathway in a potentially incorrect state if auto-transition occurred.

**Current Code Pattern:**
```csharp
// Simplified version of current implementation
var pathway = await _workflowService.GetPatientPathwayAsync(tenantId, pathwayId);
await _workflowService.OnFormSubmittedAsync(tenantId, pathwayId, request.FormId, request.Responses);
await _workflowService.EvaluateBusinessRulesAsync(tenantId, pathwayId);
// ← If auto-transition happened, need to refresh pathway state
return Ok(new { message = "Form submitted", pathwayId = pathwayId });
```

**What Should Happen:**
```csharp
// After form submission + rules evaluation
var updatedPathway = await _workflowService.GetPatientPathwayAsync(tenantId, pathwayId);
// Return updated pathway with new stage if transitioned
return Ok(new WorkflowPathwayDto { 
	...updatedPathway...,
	StageChanged = pathway.CurrentProcessStageId != updatedPathway.CurrentProcessStageId,
	NewStageName = updatedPathway.CurrentProcessStageId != pathway.CurrentProcessStageId 
		? updatedPathway.CurrentStageName 
		: null
});
```

**Action Required:** Refactor `WorkflowController.SubmitFormResponses` to refresh and return the updated pathway state, showing whether a transition occurred.

---

### 2. TaskItem Entity - Minimal Implementation

**Location:** `Domain/Entities/TaskItem.cs` (or similar)

**Issue:** `TaskItem` is currently minimal (just basic CRUD properties). It doesn't store:
- Link back to `PatientPathway`
- Link to `TaskDefinition` template
- Completion status
- Assigned provider
- Due date

**Current State:**
```csharp
public class TaskItem
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
}
```

**What's Needed:**
```csharp
public class TaskItem
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid PatientPathwayId { get; set; }      // ← Link to patient
	public Guid TaskDefinitionId { get; set; }      // ← Link to template
	public string Title { get; set; }
	public string Description { get; set; }

	// Status tracking
	public string Status { get; set; }              // Pending, InProgress, Completed
	public DateTime? DueDate { get; set; }
	public DateTime? CompletedAt { get; set; }

	// Assignment
	public Guid? AssignedToUserId { get; set; }     // Nurse/provider
	public string Priority { get; set; }            // High, Normal, Low

	// Audit
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public Guid? CompletedByUserId { get; set; }
	public string CompletionNotes { get; set; }

	// Navigation
	public PatientPathway PatientPathway { get; set; }
	public TaskDefinition TaskDefinition { get; set; }
}
```

**Impact:**
- Current `WorkflowService.GetStageTasksAsync()` returns an empty collection
- Task creation is placeholder only
- Patient UI cannot show assigned tasks
- Task completion tracking is not possible

**Action Required:** Extend `TaskItem` entity, update migration, update `WorkflowService.GetStageTasksAsync()` to create real task instances, and add task management endpoints to `WorkflowController`.

---

### 3. React Integration - No Workflow UI Yet

**Location:** `client/` directory

**Issue:** While the backend is fully functional, there is:
- No admin workflow configuration UI
- No patient workflow status dashboard
- No form submission UI for patient pathways
- No task assignment/tracking UI

**What Should Exist:**
```
/admin/workflow
  ├─ /configurations           → List program configurations
  ├─ /configurations/{id}      → Edit stages, forms, rules, templates
  └─ /configurations/{id}/new  → Create new workflow

/patient/pathways
  ├─ /                         → List active pathways
  ├─ /{id}                     → Show current stage status
  ├─ /{id}/forms               → Display required forms
  ├─ /{id}/forms/{formId}      → Submit form responses
  └─ /{id}/history             → View progression timeline

/dashboard
  ├─ /admin                    → KPI metrics (completion rates, SLA health)
  └─ /patient                  → My programs, current stage, next steps
```

**Action Required:** (After fixing TaskItem and WorkflowController)
1. Create React components for workflow admin UI
2. Create React components for patient pathway UI
3. Wire up API calls to new backend endpoints
4. Test end-to-end workflow progression through UI

---

### 4. Integration Tests - Not Started

**Location:** `BOnlineHomeAssignement.Server/` (no test project yet)

**Issue:** xUnit was added to the server project, but:
- No test project created (e.g., `BOnlineHomeAssignement.Tests`)
- No integration tests written for workflow flows
- No mock data fixtures for testing

**Recommended Test Scenarios:**
```csharp
[Fact]
public async Task EnrollPatientInProgram_CreatesPathwayWithInitialStage()
{
	// Arrange
	var patientId = Guid.NewGuid();
	var enrollment = await CreateTestEnrollment(patientId);

	// Act
	var pathway = await _workflowService.EnrollPatientInProgramAsync(
		tenantId, patientId, enrollment.Id, configId);

	// Assert
	Assert.NotNull(pathway);
	Assert.Equal(stage1.Id, pathway.CurrentProcessStageId);
}

[Fact]
public async Task SubmitForm_WithAutoAdvanceRule_TransitionsStage()
{
	// Arrange
	var pathway = await CreateTestPathway();

	// Act
	await _workflowService.OnFormSubmittedAsync(tenantId, pathway.Id, formId, responses);
	await _workflowService.EvaluateBusinessRulesAsync(tenantId, pathway.Id);

	// Assert
	var updated = await _workflowService.GetPatientPathwayAsync(tenantId, pathway.Id);
	Assert.NotEqual(stage1.Id, updated.CurrentProcessStageId);
}

[Fact]
public async Task RolePermission_NurseCannotAccessRestrictedStage()
{
	// Arrange
	var restrictedStageId = Guid.Parse("...");
	var nurseRole = "Nurse";

	// Act & Assert
	var canAccess = rolePermissionService.CanAccessStage(
		tenantId, configId, nurseRole, restrictedStageId);
	Assert.False(canAccess);
}
```

**Action Required:**
1. Create `BOnlineHomeAssignement.Tests` project (`dotnet new xunit`)
2. Add test fixtures for common setup (tenant, patient, enrollment, pathway)
3. Write integration tests for core workflow scenarios
4. Achieve ≥80% code coverage for WorkflowService

---

### 5. Form Submission Validation - Basic Only

**Location:** `WorkflowService.OnFormSubmittedAsync()`

**Issue:** Currently stores responses without strong validation:

**Current Pattern:**
```csharp
var pathway = await db.PatientPathways.FindAsync(pathwayId);
pathway.PathwayData = JsonSerializer.Serialize(new { 
	formResponses = responses,  // ← Stored as-is, minimal validation
	lastFormSubmittedAt = DateTime.UtcNow
});
await db.SaveChangesAsync();
```

**What Should Happen:**
```csharp
// 1. Get form definition to understand fields
var formDef = await db.FormDefinitions.FindAsync(request.FormId);

// 2. Validate each submitted response against field definition
foreach (var field in formDef.Fields)
{
	if (field.IsRequired && !responses.ContainsKey(field.Name))
		throw new ValidationException($"Required field missing: {field.Name}");

	if (field.Type == "email" && !Regex.IsMatch(responses[field.Name], pattern))
		throw new ValidationException($"Invalid email in field: {field.Name}");
}

// 3. Check conditional visibility rules
if (responses["hasCondition"] == "Yes" && !responses.ContainsKey("conditionDetails"))
	throw new ValidationException("Condition details required when answer is Yes");

// 4. Only then store
pathway.PathwayData = responses;
```

**Action Required:** Implement field-level validation based on `FormDefinition.FieldsDefinition` JSON schema, support complex validation rules (email, phone, regex patterns, conditional requirements).

---

### 6. Business Rule Condition Evaluation - JSON String Parsing

**Location:** `WorkflowService.EvaluateBusinessRulesAsync()`

**Issue:** Rules are stored as JSON strings and parsed via string comparison. This is fragile:

**Current Pattern:**
```csharp
var rules = await db.BusinessRules
	.Where(r => r.TriggerEvent == triggerEvent)
	.ToListAsync();

foreach (var rule in rules)
{
	// Parse JSON condition as string
	var condition = JsonDocument.Parse(rule.Condition);

	// Fragile string matching
	if (condition["formId"].GetString() == formId)
	{
		// Rule matches, execute action
	}
}
```

**Problems:**
- No schema validation for condition/action JSON
- Hard to debug when JSON structure is wrong
- No type safety
- Complex conditions (AND, OR, NOT) hard to express

**What Should Happen:**
```csharp
// 1. Typed condition classes
public class FormSubmitCondition
{
	public Guid FormId { get; set; }
	public string[] RequiredFields { get; set; }
}

// 2. Deserialization with validation
var condition = JsonSerializer.Deserialize<FormSubmitCondition>(rule.Condition);

// 3. Strongly-typed evaluation
if (condition.FormId == formId && 
	condition.RequiredFields.All(f => responses.ContainsKey(f)))
{
	// Execute action
}
```

**Action Required:** Create typed models for BusinessRule conditions/actions, add validation in `ConfigurationService` when rules are created, use typed deserialization in `EvaluateBusinessRulesAsync()`.

---

### 7. ContentTemplate Variable Substitution - Not Implemented

**Location:** `WORKFLOW_ARCHITECTURE.md` shows `{{variable}}` support, but not implemented in service

**Issue:** Templates store placeholders but don't substitute patient/workflow data:

**Current Pattern:**
```csharp
var template = new ContentTemplate
{
	Content = "Hi {{patientName}}, your {{stageName}} assessment is due by {{dueDate}}"
};
// Stored as-is, no substitution happening
```

**What Should Happen:**
```csharp
public string RenderTemplate(
	ContentTemplate template, 
	Patient patient, 
	PatientPathway pathway, 
	ProcessStage stage)
{
	var rendered = template.Content
		.Replace("{{patientName}}", patient.FirstName)
		.Replace("{{stageName}}", stage.Name)
		.Replace("{{dueDate}}", pathway.StageDueAt?.ToString("MMM dd, yyyy"))
		.Replace("{{programName}}", configuration.Program.Name)
		.Replace("{{daysRemaining}}", 
			Math.Max(0, (int)(pathway.StageDueAt - DateTime.UtcNow).TotalDays));

	return rendered;
}

// Usage when sending notification
var message = RenderTemplate(template, patient, pathway, stage);
await _notificationService.SendEmailAsync(patient.Email, template.Subject, message);
```

**Action Required:** Implement template rendering with variable substitution, add helper to inject pathway/stage/patient data, integrate into notification flow.

---

### 8. No Notification System Integration

**Location:** No reference in `WorkflowService` or anywhere

**Issue:** BusinessRules can trigger notifications but there's no implementation:

```csharp
// In rule action
{
	"type": "notification",
	"channel": "email",  // ← Not handled
	"templateId": "...",
	"recipients": ["patient", "nurse"]
}
```

**What's Needed:**
```csharp
public interface INotificationService
{
	Task SendEmailAsync(string to, string subject, string body);
	Task SendSmsAsync(string phoneNumber, string message);
	Task SendInAppNotificationAsync(Guid userId, string message);
}

// Inject and use
if (action.Type == "notification")
{
	var template = await db.ContentTemplates.FindAsync(action.TemplateId);
	var rendered = RenderTemplate(template, patient, pathway, stage);
	await _notificationService.SendEmailAsync(patient.Email, template.Subject, rendered);
}
```

**Action Required:** Create `INotificationService` interface, implement email/SMS/in-app variants, wire into `EvaluateBusinessRulesAsync()`.

---

### 9. No Patient Cohort/Stratification Logic

**Location:** `PatientPathway.PatientType` is set at enrollment but not used

**Issue:** Different patient types should follow different stage flows:

```csharp
// Patient Type: "Initial" vs "Chronic" vs "Advanced"
// Should result in different configurations/rules
// Currently not implemented
```

**What Should Happen:**
```csharp
// Enroll with patient type
var pathway = await _workflowService.EnrollPatientInProgramAsync(
	tenantId, patientId, enrollmentId, configId, 
	patientType: "Chronic"  // ← Affects which forms/rules apply
);

// In service, check patient type when:
// 1. Loading required forms (filter by PatientType)
// 2. Creating tasks (filter by PatientType)
// 3. Evaluating rules (conditional on PatientType)

public async Task<IList<FormDefinition>> GetRequiredFormsAsync(
	Guid tenantId, Guid pathwayId)
{
	var pathway = await db.PatientPathways.FindAsync(pathwayId);

	return await db.FormDefinitions
		.Where(f => f.ProgramConfigurationId == pathway.ProgramConfigurationId
				 && f.ApplicableStages.Contains(currentStage.Name)
				 && f.ApplicablePatientTypes.Contains(pathway.PatientType))  // ← Check type
		.ToListAsync();
}
```

**Action Required:** Add `ApplicablePatientTypes` field to FormDefinition/TaskDefinition/BusinessRule, update queries to filter by patient type.

---

### 10. ProcessStage.AllowedStatuses Not Enforced

**Location:** `WorkflowService.UpdateStageStatusAsync()`

**Issue:** ProcessStage defines allowed status transitions but they're not validated:

```csharp
// ProcessStage.AllowedStatuses = "Active,Pending,Completed"
// But when updating, no check that new status is in allowed set
```

**What Should Happen:**
```csharp
public async Task UpdateStageStatusAsync(
	Guid tenantId, Guid pathwayId, string newStatus)
{
	var pathway = await db.PatientPathways.FindAsync(pathwayId);
	var stage = await db.ProcessStages.FindAsync(pathway.CurrentProcessStageId);

	if (!stage.AllowedStatuses.Split(',').Contains(newStatus))
		throw new InvalidOperationException(
			$"Status '{newStatus}' not allowed for stage '{stage.Name}'. " +
			$"Allowed: {stage.AllowedStatuses}");

	pathway.CurrentStatus = newStatus;
	await db.SaveChangesAsync();
}
```

**Action Required:** Add status validation in `UpdateStageStatusAsync()`, add validation test.

---

## Recommended Priority Order

### P0 (Blocking Use)
1. Fix `WorkflowController.SubmitFormResponses` return state
2. Implement typed BusinessRule condition parsing
3. Extend `TaskItem` entity and create real task instances

### P1 (High Value for Testing)
4. Add integration tests with proper fixtures
5. Implement form field validation
6. Implement ProcessStage.AllowedStatuses enforcement

### P2 (Nice to Have)
7. Add `INotificationService` and template rendering
8. Implement patient cohort/stratification logic
9. Create React admin UI for workflow configuration
10. Create React patient UI for pathway progression

### P3 (Future)
11. Rules engine with complex logic (AND/OR/NOT conditions)
12. Configuration versioning and rollback
13. KPI dashboards and analytics
14. Phase 4: Event-driven integration with RabbitMQ

---

## Testing Notes

**Until TaskItem and form validation are fixed**, use these workarounds for manual testing:

```bash
# 1. Create configuration (works)
POST /api/programconfiguration/create
{ "programId": "11111111-2222-...", "workflowType": "Linear" }

# 2. Add stages (works)
POST /api/programconfiguration/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/stages
{ "name": "Stage 1", "order": 1, "durationDays": 7, ... }

# 3. Enroll patient (works, creates pathway)
POST /api/workflow/patients/{patientId}/enroll
{ "enrollmentId": "...", "programConfigurationId": "...", "patientType": "Chronic" }

# 4. Get pathway (works)
GET /api/workflow/pathways/{pathwayId}

# 5. Submit form (works, but doesn't return updated state correctly)
POST /api/workflow/pathways/{pathwayId}/submit-form
{ "formId": "44444444-aaaa-bbbb-cccc-444444444444", "responses": {...} }

# 6. Check can advance (works)
GET /api/workflow/pathways/{pathwayId}/can-advance

# 7. Transition manually (works)
POST /api/workflow/pathways/{pathwayId}/transition-next
```

---

## File References

Key files that need updates:
```
BOnlineHomeAssignement.Server/
├─ Controllers/WorkflowController.cs         (FIX: return state)
├─ Application/Services/WorkflowService.cs   (ADD: validation, notifications)
├─ Domain/Entities/TaskItem.cs               (EXTEND: add fields)
├─ Infrastructure/Persistence/AppDbContext.cs (UPDATE: TaskItem mapping)
├─ Migrations/*.cs                           (NEW: TaskItem extension)
├─ Application/Validation/*.cs               (NEW: Form validators)
└─ Exceptions/*.cs                           (NEW: Custom exceptions)

BOnlineHomeAssignement.Tests/                (NEW: Create project)
├─ Fixtures/WorkflowTestFixture.cs           (Setup test data)
├─ WorkflowServiceTests.cs
├─ FormDefinitionTests.cs
└─ BusinessRuleTests.cs

BOnlineHomeAssignement.Client/               (NEW: Create components)
├─ src/components/Workflow/
│  ├─ ProgramConfigurationEditor.tsx
│  ├─ PatientPathwayDashboard.tsx
│  └─ FormSubmissionComponent.tsx
└─ src/services/workflowService.ts           (UPDATE: call new endpoints)
```

---

## Session Handoff Summary

**To the next developer:**

1. The workflow system is **architecturally complete and builds successfully**
2. **Core services work** but have placeholder/incomplete features listed above
3. **Before shipping** → Fix P0 items (return states, rule parsing, TaskItem)
4. **Before production** → Add P1 items (tests, validation)
5. **For Phase 4** → Implement notifications, event publishing, KPI dashboards

All code compiles. No build errors. Multi-tenant isolation enforced. Ready for the fixes and enhancements listed in **Outstanding Work & Next Steps** above.
