# Phase 3.5: Configurable Workflow Architecture

## Overview

The configurable workflow system allows healthcare organizations to define custom patient care programs without code changes. Each customer can define:

- **Program Workflows** - Multi-stage care pathways
- **Patient Types** - Cohort-based differentiation (Initial, Advanced, Chronic, etc.)
- **Process Stages** - Sequential workflow steps with status tracking and SLAs
- **Forms & Questionnaires** - Dynamic data collection with visibility conditions
- **Business Rules** - If-then automation for stage transitions, tasks, and notifications
- **Content Templates** - Reusable messages with variable substitution
- **Role-Based Access** - Granular permissions per stage and operation type

## Architecture Components

### Domain Entities

#### ProgramConfiguration
The root entity representing a customized workflow for a program. Each program can have multiple configurations for A/B testing or versioning.

```csharp
public class ProgramConfiguration
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }           // Multi-tenant isolation
	public Guid ProgramId { get; set; }          // Links to Program entity
	public string WorkflowType { get; set; }     // Linear, Branching, Custom
	public string? PatientTypes { get; set; }    // CSV: "Initial,Advanced,Chronic"
	public bool IsActive { get; set; }
	// Relationships: ProcessStages, PatientPathways, Forms, Rules, Templates
}
```

#### ProcessStage
Represents a step in the workflow. Each stage can have multiple allowed statuses and triggers automated tasks.

```csharp
public class ProcessStage
{
	public Guid Id { get; set; }
	public string Name { get; set; }                    // "Initial Assessment"
	public int Order { get; set; }                      // Sequence in workflow
	public int? DurationDays { get; set; }              // SLA: days to complete
	public string? AllowedStatuses { get; set; }        // "Active,Pending,Completed"
	public string? RequiredFormIds { get; set; }        // CSV of form IDs
	public string? TriggeredTaskIds { get; set; }       // Auto-create tasks
	public string? AutoAdvanceConditions { get; set; }  // JSON conditions
}
```

#### PatientPathway
Tracks a patient's progression through a program configuration. Multiple pathways can exist for one patient across different programs.

```csharp
public class PatientPathway
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }                 // Which patient
	public Guid EnrollmentId { get; set; }              // Links to Enrollment
	public Guid ProgramConfigurationId { get; set; }    // Which program config
	public Guid CurrentProcessStageId { get; set; }     // Current stage
	public string CurrentStatus { get; set; }           // "Active", "Pending", etc.
	public string? PatientType { get; set; }            // "Chronic", "Advanced"
	public string? PathwayData { get; set; }            // JSON form responses
	public DateTime EnteredStageAt { get; set; }
	public DateTime? StageDueAt { get; set; }           // SLA expiry
	public DateTime? CompletedAt { get; set; }
	public bool IsActive { get; set; }
}
```

#### FormDefinition
Dynamic form definitions with conditional visibility and submit actions.

```csharp
public class FormDefinition
{
	public Guid Id { get; set; }
	public string Title { get; set; }                       // "Initial Assessment"
	public string FieldsDefinition { get; set; }            // JSON field array
	public string? ApplicableStages { get; set; }           // CSV of stage names
	public string? VisibilityConditions { get; set; }       // JSON: show if...
	public string? OnSubmitActions { get; set; }            // JSON: what happens next
	public bool IsRequired { get; set; }
	// FieldsDefinition example:
	// [
	//   {"name":"medicalHistory","type":"textarea","required":true},
	//   {"name":"concerns","type":"checkbox","options":["Mental","Physical"]}
	// ]
}
```

#### TaskDefinition
Template for tasks that can be automatically triggered at stages.

```csharp
public class TaskDefinition
{
	public Guid Id { get; set; }
	public string Title { get; set; }                   // "Initial Nurse Assessment"
	public string TaskType { get; set; }                // "Manual", "Automated", "Notification"
	public string? AssignToRole { get; set; }           // "Nurse", "Doctor"
	public int DueDays { get; set; }                    // Days to complete
	public bool AutoEscalate { get; set; }              // Escalate if overdue
	public Guid? ContentTemplateId { get; set; }        // Link to notification template
}
```

#### BusinessRule
Configurable rules that define workflow automation.

```csharp
public class BusinessRule
{
	public Guid Id { get; set; }
	public string Name { get; set; }                    // "Auto-advance on form submit"
	public string RuleType { get; set; }                // "StageTransition", "TaskTrigger", "Notification"
	public string TriggerEvent { get; set; }            // "OnFormSubmit", "Daily", "OnStageEntry"
	public string Condition { get; set; }               // JSON: when does this apply?
	public string Action { get; set; }                  // JSON: what should happen?
	public int Priority { get; set; }                   // 1=highest, evaluation order
}
```

#### ContentTemplate
Reusable message templates with variable substitution.

```csharp
public class ContentTemplate
{
	public Guid Id { get; set; }
	public string Name { get; set; }                    // "Initial Assessment Due"
	public string ContentType { get; set; }             // "Email", "SMS", "InApp"
	public string Subject { get; set; }                 // Email subject
	public string Content { get; set; }                 // Template with {{variables}}
	public string? SupportedVariables { get; set; }     // CSV of var names
	// Example: "Dear {{patientName}}, your {{stageName}} is due {{dueDate}}"
}
```

#### RolePermission
Define roles and their access/permissions within a program workflow.

```csharp
public class RolePermission
{
	public Guid Id { get; set; }
	public string RoleName { get; set; }                // "Doctor", "Nurse", "Patient", "Admin"
	public string? AccessibleStages { get; set; }       // CSV of stage IDs
	public string? Permissions { get; set; }            // CSV: Read, Create, Edit, Delete
	public bool CanApproveTransitions { get; set; }     // Can approve stage changes
	public bool CanOverrideRules { get; set; }          // Can bypass automation
}
```

### Services

#### IConfigurationService
Manages program configuration definitions. Allows customers to configure their workflows.

**Key Methods:**
- `CreateProgramConfigurationAsync()` - Create workflow for a program
- `AddProcessStageAsync()` - Add a stage to workflow
- `CreateFormDefinitionAsync()` - Define a form/questionnaire
- `CreateBusinessRuleAsync()` - Define automation rules
- `CreateContentTemplateAsync()` - Create message templates
- `ExportConfigurationAsync()` - Export full config as JSON
- `ImportConfigurationAsync()` - Import config from JSON

#### IWorkflowService
Manages patient pathway progression through configured workflows.

**Key Methods:**
- `EnrollPatientInProgramAsync()` - Start patient in program
- `TransitionToNextStageAsync()` - Move to next stage
- `TransitionToStageAsync()` - Admin override transition
- `UpdateStageStatusAsync()` - Update current status
- `GetPatientPathwayAsync()` - Get patient's current state
- `CanAdvanceStageAsync()` - Check if conditions met
- `EvaluateBusinessRulesAsync()` - Run automation rules
- `OnFormSubmittedAsync()` - Handle form data and trigger actions
- `CompletePathwayAsync()` - Discharge patient

### API Endpoints

#### Program Configuration Endpoints

```
POST   /api/programconfiguration/create
	   Create a program configuration

GET    /api/programconfiguration/{id}
	   Get configuration details

POST   /api/programconfiguration/{id}/stages
	   Add a process stage

GET    /api/programconfiguration/{id}/stages
	   List all stages (ordered)

POST   /api/programconfiguration/{id}/forms
	   Create a form definition

GET    /api/programconfiguration/{id}/forms
	   List all forms

POST   /api/programconfiguration/{id}/rules
	   Create a business rule

GET    /api/programconfiguration/{id}/rules
	   List all rules (by priority)

POST   /api/programconfiguration/{id}/templates
	   Create content template

GET    /api/programconfiguration/{id}/templates
	   List templates (optional filter: ?contentType=Email)

POST   /api/programconfiguration/{id}/roles
	   Create role definition

GET    /api/programconfiguration/{id}/roles
	   List all roles

GET    /api/programconfiguration/{id}/export
	   Export full configuration as JSON

POST   /api/programconfiguration/{programId}/import
	   Import configuration from JSON
```

#### Workflow/Pathway Endpoints

```
POST   /api/workflow/patients/{id}/enroll
	   Enroll patient in program → creates initial pathway

GET    /api/workflow/patients/{id}/pathways
	   Get all pathways for patient

GET    /api/workflow/patients/{id}/programs/{progId}
	   Get patient's pathway in specific program

POST   /api/workflow/pathways/{id}/transition-next
	   Advance to next stage (if conditions met)

POST   /api/workflow/pathways/{id}/transition-to/{stageId}
	   Admin override: jump to specific stage

GET    /api/workflow/pathways/{id}/can-advance
	   Check readiness, get blocking reasons

PATCH  /api/workflow/pathways/{id}/status
	   Update current status within stage

GET    /api/workflow/pathways/{id}/required-forms
	   List forms for current stage

POST   /api/workflow/pathways/{id}/submit-form
	   Submit form responses, trigger actions

GET    /api/workflow/pathways/{id}/tasks
	   List pending tasks for current stage

POST   /api/workflow/pathways/{id}/evaluate-rules
	   Manually trigger rule evaluation

POST   /api/workflow/pathways/{id}/complete
	   Mark patient as discharged
```

## Example: Chronic Care Program Configuration

### Workflow Definition (JSON)
```json
{
  "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "programId": "11111111-2222-3333-4444-555555555555",
  "workflowType": "Linear",
  "patientTypes": ["Initial", "Chronic", "Complex"],
  "stages": [
	{
	  "id": "stage-1",
	  "name": "Initial Assessment",
	  "order": 1,
	  "durationDays": 7,
	  "allowedStatuses": "Active,Pending,Completed",
	  "requiredFormIds": ["form-health-intake"],
	  "triggeredTaskIds": ["task-nurse-assessment"]
	},
	{
	  "id": "stage-2",
	  "name": "Treatment Planning",
	  "order": 2,
	  "durationDays": 14,
	  "requiredFormIds": ["form-treatment-approval"]
	},
	{
	  "id": "stage-3",
	  "name": "Follow-Up (30 days)",
	  "order": 3,
	  "durationDays": 30,
	  "triggeredTaskIds": ["task-monthly-checkup"]
	}
  ],
  "forms": [
	{
	  "id": "form-health-intake",
	  "title": "Health History & Current Status",
	  "fields": [
		{"name": "chronicConditions", "type": "checkbox", "required": true},
		{"name": "medications", "type": "textarea", "required": true},
		{"name": "allergies", "type": "text", "required": false}
	  ],
	  "applicableStages": ["Initial Assessment"],
	  "onSubmitActions": {"type": "autoAdvance", "ifAllowed": true}
	}
  ],
  "rules": [
	{
	  "name": "Auto-advance if assessment complete",
	  "triggerEvent": "OnFormSubmit",
	  "condition": {"formId": "form-health-intake"},
	  "action": {"type": "transition", "toStage": "stage-2"},
	  "priority": 1
	},
	{
	  "name": "Alert if assessment overdue",
	  "triggerEvent": "Daily",
	  "condition": {"stage": "stage-1", "daysOverdue": 3},
	  "action": {"type": "sendNotification", "templateId": "email-overdue"},
	  "priority": 5
	}
  ],
  "templates": [
	{
	  "id": "email-overdue",
	  "contentType": "Email",
	  "subject": "Your health assessment is overdue",
	  "content": "Hi {{patientName}}, your {{stageName}} was due on {{dueDate}}. Please complete it ASAP."
	}
  ],
  "roles": [
	{
	  "name": "Patient",
	  "canViewPathways": true,
	  "permissions": ["Read", "SubmitForms"]
	},
	{
	  "name": "Nurse",
	  "accessibleStages": ["stage-1", "stage-2", "stage-3"],
	  "permissions": ["Read", "Create", "Edit"],
	  "canApproveTransitions": true
	}
  ]
}
```

## Multi-Tenant Data Isolation

All entities include `TenantId` for strict isolation:

```csharp
// Only patient pathways for a specific tenant
GET /api/workflow/patients/{id}/pathways
Headers: X-Tenant-Id: {tenantId}
// Returns only pathways where pathway.TenantId == tenantId

// Only configurations for a specific tenant
db.ProgramConfigurations
  .Where(pc => pc.TenantId == tenantId)
  .Where(pc => pc.ProgramId == programId)
```

## JSON Configuration Format

For configuration import/export, all entities serialize to JSON. This enables:

- **Config as Code** - Store configs in version control
- **Config Sharing** - Import pre-built workflows for new customers
- **A/B Testing** - Export config, modify, re-import as new version
- **Recovery** - Backup and restore customer configurations

## Future Enhancements (Phase 4)

### Event-Driven Workflow Automation
Replace JSON condition/action evaluation with a proper rules engine:
```csharp
// Integrate with RabbitMQ for event publishing
// FormSubmitted -> RabbitMQ -> WorkflowService -> EvaluateRules -> PublishActions
```

### Workflow Visual Designer
Build React UI for customers to drag-drop workflow diagrams:
- Stage builder with conditional branching
- Form designer with field types
- Rule builder with visual condition editor
- Template manager with preview

### Advanced Tracking
- KPI dashboards (time-in-stage, completion rates, pathways by type)
- Metrics-driven automation (e.g., "if 50% of patients skip this form, mark it optional")
- Patient cohort analysis

### Audit & Compliance
- Full audit trail of configuration changes
- Compliance report generation
- Configuration versioning with rollback

## Demo Data

The seed data creates a sample "Chronic Care Program" with:
- **3 Stages**: Initial Assessment → Treatment Planning → Follow-Up
- **2 Forms**: Health intake, Treatment approval
- **2 Rules**: Auto-advance, Overdue reminder
- **3 Roles**: Patient (read-only), Nurse (create/edit/approve), Admin (full)
- **2 Email Templates**: Assessment reminder, Plan ready notification

## Testing the Workflow

### 1. Get Configuration
```bash
curl -X GET "http://localhost:5000/api/programconfiguration/{configId}" \
  -H "X-Tenant-Id: 8a3d9c2a-1111-4f3b-8c2e-000000000001"
```

### 2. Enroll Patient
```bash
curl -X POST "http://localhost:5000/api/workflow/patients/{patientId}/enroll" \
  -H "X-Tenant-Id: {tenantId}" \
  -H "Content-Type: application/json" \
  -d '{
	"enrollmentId": "{enrollmentId}",
	"programConfigurationId": "{configId}",
	"patientType": "Chronic"
  }'
```

### 3. Get Patient Pathways
```bash
curl -X GET "http://localhost:5000/api/workflow/patients/{patientId}/pathways" \
  -H "X-Tenant-Id: {tenantId}"
```

### 4. Get Required Forms
```bash
curl -X GET "http://localhost:5000/api/workflow/pathways/{pathwayId}/required-forms" \
  -H "X-Tenant-Id: {tenantId}"
```

### 5. Submit Form
```bash
curl -X POST "http://localhost:5000/api/workflow/pathways/{pathwayId}/submit-form" \
  -H "X-Tenant-Id: {tenantId}" \
  -H "Content-Type: application/json" \
  -d '{
	"formId": "{formId}",
	"responses": {
	  "medicalHistory": "Type 2 diabetes, hypertension",
	  "medications": "Metformin 500mg twice daily"
	}
  }'
```

### 6. Check If Can Advance
```bash
curl -X GET "http://localhost:5000/api/workflow/pathways/{pathwayId}/can-advance" \
  -H "X-Tenant-Id: {tenantId}"
Response: {
  "canAdvance": true,
  "blockingReasons": []
}
```

### 7. Transition to Next Stage
```bash
curl -X POST "http://localhost:5000/api/workflow/pathways/{pathwayId}/transition-next" \
  -H "X-Tenant-Id: {tenantId}" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Assessment complete and approved by nurse"}'
```

## Key Design Principles

1. **Zero Hardcoding**: All workflow logic is configuration-driven in the database
2. **Multi-Tenant**: Strict tenant isolation ensures customer data never leaks
3. **Extensible**: JSON-based conditions/actions allow complex logic without code changes
4. **Auditable**: All transitions logged with timestamps and notes
5. **Flexible**: Linear, branching, or custom workflows via configuration
6. **User-Centric**: Patients see only forms/info for their current stage
7. **Role-Based**: Access controlled per stage and operation type
8. **SLA-Driven**: Stages have due dates, overdue alerts triggered by rules

## Files Modified/Created

```
Domain/Entities/WorkflowEntities.cs          ← 8 new entities
Infrastructure/Persistence/AppDbContext.cs   ← DbSet registrations + EF config
Infrastructure/Persistence/SeedData.cs       ← Sample workflow config
Application/Services/WorkflowService.cs      ← Pathway progression logic
Application/Services/ConfigurationService.cs ← Workflow configuration management
Application/DTOs/Configuration/ProgramConfigurationDtos.cs ← Request/response DTOs
Controllers/ProgramConfigurationController.cs ← REST API for config management
Controllers/WorkflowController.cs             ← REST API for patient pathways
Migrations/AddConfigurableWorkflowEntities.cs ← EF migration
Program.cs                                    ← Service registration
```

## Next: React UI for Configuration

The workflow configuration requires a React admin interface for customers to:
1. Create/edit stages
2. Design forms with conditional visibility
3. Define business rules
4. Create message templates
5. Set role permissions

This would be a separate `WorkflowConfigurationUI` component set in the client.
