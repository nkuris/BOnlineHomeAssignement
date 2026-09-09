# Extended Requirements: Timeline, Events, Triggers & Forms

## Translation of New Requirements

### 1. Timeline, Events & Tasks (ציר זמן, אירועים ומשימות)

**Requirement:**
Each patient has a sequence of events and actions. Timeline includes: business events, tasks, user actions, forms, calls, messages, questionnaire responses, status changes. Not all technical events need to be shown to patient.

**Example Timeline:**
```
01/01  → Lead Created
02/01  → Consent Sent + Consent Signed
03/01  → Patient Enrolled + Nurse Assigned
10/01  → Follow-up Task Due + Nurse Call Completed
20/01  → Questionnaire Sent
21/01  → Questionnaire Completed
```

**Task Requirements:**
- Patient/Enrollment
- Program
- Assigned User
- Type, Status, Due Date, Priority
- Creation Source (Manual/Automatic)
- Outcome, Completion Date
- Can be created manually or automatically

**Gap in Current Implementation:** 
❌ Timeline entity missing
❌ Event tracking incomplete
❌ TaskItem too minimal (no Due Date, Priority, AssignedUser, Outcome, etc.)

---

### 2. Processes & Triggers (תהליכים וטריגרים)

**Requirement:**
System must support automatic actions triggered by time or events. Must distinguish between two types and handle: Retry, duplicates, failures, timing.

**Time-Based Examples:**
- 7 days after Enrollment → Create Follow-up task
- 30 days after questionnaire → Send another questionnaire
- 14 days in stage → Automatic alert to nurse

**Event-Based Examples:**
- After Consent signed → Move to Enrollment
- Specific questionnaire response → Create task for nurse
- Task completed → Auto-advance stage
- Form submitted → Evaluate conditionality

**Gap in Current Implementation:**
❌ Trigger system incomplete
❌ Time-based triggers not implemented
❌ Event-based triggers basic only
❌ No retry/duplicate/failure handling

---

### 3. Forms, Questionnaires & Communication (טפסים, שאלונים ותקשורת)

**Requirement:**
Programs include: Registration forms, Consent, Medical Intake, Questionnaires, Follow-up forms. Form responses can:
- Affect pathway progression
- Create new task/event
- Trigger communication

Communication channels: SMS, WhatsApp, Email, Voice (design for integration, don't implement).

**Gap in Current Implementation:**
❌ Form response conditional logic not implemented
❌ Communication system not designed
❌ No questionnaire/consent tracking

---

## Gap Analysis: Current vs Required

| Feature | Current | Required | Status |
|---------|---------|----------|--------|
| **Timeline/Events** | PatientPathway only | Full timeline of all events | ❌ Missing |
| **Event Entity** | None | WorkflowEvent (type, timestamp, source) | ❌ Missing |
| **Tasks** | Minimal TaskItem | Full task with all 10 properties | ⚠️ Incomplete |
| **Time-Based Triggers** | None | Scheduled trigger processor | ❌ Missing |
| **Event-Based Triggers** | Basic BusinessRule | Full trigger system with patterns | ⚠️ Basic |
| **Form Response Logic** | Accept responses | Conditional progression + task creation | ⚠️ Incomplete |
| **Questionnaire Tracking** | Generic form | Specific questionnaire entity | ❌ Missing |
| **Communication Design** | None | Template + Channel abstraction | ❌ Missing |
| **Retry/Duplicate Handling** | None | Trigger execution tracking | ❌ Missing |

---

## Implementation Plan to Complete These Requirements

### Phase 1: Entities (Add Missing Models)

#### A. Timeline & Events
```csharp
// NEW: WorkflowEvent - Every action/state change becomes an event
public class WorkflowEvent
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid PatientPathwayId { get; set; }
	public Guid? PatientId { get; set; }

	// WHAT happened
	public string EventType { get; set; }  // "FormSubmitted", "TaskCreated", "StageEntered", etc.
	public string EventSource { get; set; } // "Manual", "Automatic", "System"
	public DateTime OccurredAt { get; set; }

	// DETAILS of what happened
	public string Data { get; set; } // JSON: form responses, task details, etc.
	public string Description { get; set; } // Human-readable: "Patient submitted Health Intake form"

	// VISIBILITY to patient
	public bool VisibleToPatient { get; set; } // Not all technical events shown

	// NAVIGATION
	public PatientPathway PatientPathway { get; set; }
}

// NEW: Timeline view (sequence of events)
public class PatientTimeline
{
	public List<WorkflowEvent> Events { get; set; }  // Ordered by OccurredAt
	// API response groups by date, filters by VisibleToPatient
}
```

#### B. Enhanced TaskItem
```csharp
// MODIFY: TaskItem - Currently minimal, needs full properties
public class TaskItem
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	// LINKS
	public Guid PatientPathwayId { get; set; }
	public Guid? PatientId { get; set; }
	public Guid? EnrollmentId { get; set; }
	public Guid ProgramId { get; set; }
	public Guid? TaskDefinitionId { get; set; }  // Template that created it

	// CORE PROPERTIES
	public string Title { get; set; }
	public string Description { get; set; }
	public string Type { get; set; }  // "Nurse_Call", "Patient_Questionnaire", "Doctor_Review", etc.

	// ASSIGNMENT
	public Guid? AssignedToUserId { get; set; }  // Nurse, Doctor, etc.
	public string AssignedToRole { get; set; }   // "Nurse", "Doctor", "Admin"

	// TIMING
	public DateTime CreatedAt { get; set; }
	public DateTime DueDate { get; set; }
	public DateTime? CompletedAt { get; set; }
	public int? ReminderDaysBefore { get; set; }  // Send reminder 2 days before due

	// STATUS & PRIORITY
	public string Status { get; set; }  // "Pending", "InProgress", "Completed", "Overdue"
	public string Priority { get; set; }  // "High", "Normal", "Low"

	// SOURCE & OUTCOME
	public string CreationSource { get; set; }  // "Manual", "EventTriggered", "TimeTriggered", "FormResponse"
	public string Outcome { get; set; }  // "Completed", "Skipped", "Failed", "Escalated"
	public string CompletionNotes { get; set; }  // What was the result?

	// AUDIT
	public Guid? CreatedByUserId { get; set; }
	public Guid? CompletedByUserId { get; set; }

	// NAVIGATION
	public PatientPathway PatientPathway { get; set; }
	public TaskDefinition TaskDefinition { get; set; }
}
```

#### C. Questionnaire Tracking
```csharp
// NEW: Questionnaire - Specific form type with tracking
public class Questionnaire
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid ProgramConfigurationId { get; set; }

	// TEMPLATE
	public string Name { get; set; }  // "Health Assessment", "Follow-up Survey"
	public string Description { get; set; }
	public Guid FormDefinitionId { get; set; }  // Points to form schema

	// SCHEDULING
	public string RecurrencePattern { get; set; }  // "Once", "Weekly", "Monthly", "DaysAfterEvent"
	public int? RecurrenceDays { get; set; }  // If DaysAfterEvent: 30 days after this questionnaire
	public DateTime? NextScheduledDate { get; set; }

	// CONFIG
	public bool RequiresCompletion { get; set; }  // Block progression if not completed?
	public int TimeoutDays { get; set; }  // 7 days to complete before overdue

	public FormDefinition FormDefinition { get; set; }
}

// NEW: QuestionnaireResponse - Track patient's answers
public class QuestionnaireResponse
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid PatientPathwayId { get; set; }
	public Guid QuestionnaireId { get; set; }

	public DateTime SentAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public string Status { get; set; }  // "Sent", "Opened", "Completed", "Abandoned"

	// RESPONSES
	public string Responses { get; set; }  // JSON: {question1: answer1, ...}

	// OUTCOME
	public string TriggeredActions { get; set; }  // JSON: what actions resulted from this
	public DateTime? NextQuestionnaireScheduledFor { get; set; }

	public Questionnaire Questionnaire { get; set; }
	public PatientPathway PatientPathway { get; set; }
}
```

#### D. Trigger Execution Tracking
```csharp
// NEW: TriggerExecution - Track when/if triggers run, retry, fail
public class TriggerExecution
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid BusinessRuleId { get; set; }  // Which rule was triggered
	public Guid? PatientPathwayId { get; set; }  // Which patient affected

	// TRIGGER INFO
	public string TriggerType { get; set; }  // "TimeBasedTrigger", "EventBasedTrigger"
	public string TriggerName { get; set; }  // "7DaysAfterEnrollment", "OnConsentSigned"

	// EXECUTION
	public DateTime ScheduledFor { get; set; }  // When should it run?
	public DateTime? ExecutedAt { get; set; }  // When did it actually run?
	public string Status { get; set; }  // "Pending", "Running", "Completed", "Failed", "Retrying"

	// FAILURE HANDLING
	public int AttemptCount { get; set; }  // How many times tried?
	public int MaxRetries { get; set; }  // Retry up to N times
	public string LastError { get; set; }  // Error message if failed
	public DateTime? NextRetryAt { get; set; }  // When to retry?

	// PREVENTION OF DUPLICATES
	public string ExecutionKey { get; set; }  // Unique: "rule-{id}_pathway-{id}_{date}"
	public bool IsDuplicate { get; set; }  // Was this already executed today?

	// RESULT
	public string CreatedEntities { get; set; }  // JSON: IDs of tasks/events created

	public BusinessRule BusinessRule { get; set; }
}

// NEW: CommunicationMessage - Prepare for sending, track delivery
public class CommunicationMessage
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }
	public Guid PatientId { get; set; }

	// CHANNEL
	public string Channel { get; set; }  // "Email", "SMS", "WhatsApp", "Voice", "InApp"
	public string RecipientAddress { get; set; }  // Email, phone number, etc.

	// CONTENT
	public Guid? ContentTemplateId { get; set; }  // Which template?
	public string RenderedContent { get; set; }  // Actual message sent (after {{variable}} substitution)
	public string Subject { get; set; }  // For Email

	// TRIGGER
	public Guid? TriggerExecutionId { get; set; }  // Which trigger caused this?

	// DELIVERY
	public DateTime CreatedAt { get; set; }
	public DateTime? ScheduledFor { get; set; }
	public DateTime? SentAt { get; set; }
	public string Status { get; set; }  // "Pending", "Scheduled", "Sent", "Delivered", "Failed"
	public string DeliveryError { get; set; }

	// TRACKING
	public DateTime? OpenedAt { get; set; }  // For trackable messages
	public int ClickCount { get; set; }  // For messages with links

	public Patient Patient { get; set; }
	public TriggerExecution TriggerExecution { get; set; }
}
```

---

### Phase 2: Trigger System (Core Logic)

#### A. Time-Based Trigger Service
```csharp
public interface ITimeBasedTriggerService
{
	// Run periodically (e.g., every minute via background job)
	Task ProcessPendingTriggersAsync(Guid tenantId);

	// Example triggers to process:
	// - 7 days since Enrollment → Create Follow-up Task
	// - 30 days since last Questionnaire → Send new Questionnaire
	// - 14 days in Stage → Send alert to nurse
}

public class TimeBasedTriggerService : ITimeBasedTriggerService
{
	public async Task ProcessPendingTriggersAsync(Guid tenantId)
	{
		// 1. Get all time-based business rules for this tenant
		var timeBasedRules = await db.BusinessRules
			.Where(r => r.TenantId == tenantId 
					 && r.TriggerType == "TimeBased"
					 && r.IsActive)
			.ToListAsync();

		// 2. For each rule, find which patients it should apply to
		foreach (var rule in timeBasedRules)
		{
			// Example rule: "7 days after Enrollment, create Follow-up task"
			if (rule.TriggerName == "DaysAfterEnrollment")
			{
				int days = rule.TriggerParameters["days"];  // 7

				// Find pathways where enrollment was 7 days ago
				var pathways = await db.PatientPathways
					.Where(p => p.TenantId == tenantId
							 && p.Enrollment.EnrolledAt <= DateTime.UtcNow.AddDays(-days)
							 && !AlreadyExecuted(rule.Id, p.Id))  // Prevent duplicate
					.ToListAsync();

				foreach (var pathway in pathways)
				{
					await ExecuteTriggerAsync(rule, pathway);
				}
			}
		}
	}

	private async Task ExecuteTriggerAsync(BusinessRule rule, PatientPathway pathway)
	{
		var execution = new TriggerExecution
		{
			Id = Guid.NewGuid(),
			TenantId = rule.TenantId,
			BusinessRuleId = rule.Id,
			PatientPathwayId = pathway.Id,
			TriggerType = "TimeBasedTrigger",
			ScheduledFor = DateTime.UtcNow,
			Status = "Running"
		};

		try
		{
			// Execute the action (create task, send message, etc.)
			var createdEntities = await ExecuteActionAsync(rule.Action, pathway);

			execution.ExecutedAt = DateTime.UtcNow;
			execution.Status = "Completed";
			execution.CreatedEntities = JsonSerializer.Serialize(createdEntities);

			// Record the event
			await db.WorkflowEvents.AddAsync(new WorkflowEvent
			{
				Id = Guid.NewGuid(),
				TenantId = rule.TenantId,
				PatientPathwayId = pathway.Id,
				EventType = "TimeBasedTriggerExecuted",
				EventSource = "Automatic",
				OccurredAt = DateTime.UtcNow,
				Description = rule.TriggerName,
				VisibleToPatient = false  // Technical event
			});
		}
		catch (Exception ex)
		{
			execution.Status = "Failed";
			execution.LastError = ex.Message;
			execution.NextRetryAt = DateTime.UtcNow.AddMinutes(5);  // Retry in 5 min
		}

		await db.TriggerExecutions.AddAsync(execution);
		await db.SaveChangesAsync();
	}
}
```

#### B. Event-Based Trigger Service
```csharp
public interface IEventBasedTriggerService
{
	// Called when an event occurs (e.g., "ConsentSigned")
	Task ProcessEventTriggersAsync(Guid tenantId, string eventType, WorkflowEvent @event);
}

public class EventBasedTriggerService : IEventBasedTriggerService
{
	public async Task ProcessEventTriggersAsync(
		Guid tenantId, 
		string eventType, 
		WorkflowEvent @event)
	{
		// 1. Get all rules triggered by this event type
		var eventRules = await db.BusinessRules
			.Where(r => r.TenantId == tenantId
					 && r.TriggerType == "EventBased"
					 && r.TriggerEvent == eventType
					 && r.IsActive)
			.ToListAsync();

		// 2. Execute each rule if conditions match
		foreach (var rule in eventRules)
		{
			// Example: "If form response = 'severe', create urgent task"
			if (EvaluateCondition(rule.Condition, @event.Data))
			{
				await ExecuteTriggerAsync(rule, @event.PatientPathwayId);
			}
		}
	}

	private bool EvaluateCondition(string conditionJson, string eventData)
	{
		// Deserialize and evaluate condition against event data
		var condition = JsonDocument.Parse(conditionJson);
		var data = JsonDocument.Parse(eventData);

		// Example condition: {"responseField": "severity", "equals": "severe"}
		var field = condition["responseField"].GetString();
		var expectedValue = condition["equals"].GetString();
		var actualValue = data[field]?.GetString();

		return actualValue == expectedValue;
	}
}
```

---

### Phase 3: Form Response Handler

```csharp
public interface IFormResponseService
{
	Task<FormSubmissionResult> SubmitFormAsync(
		Guid tenantId,
		Guid pathwayId,
		Guid formId,
		Dictionary<string, string> responses);
}

public class FormResponseService : IFormResponseService
{
	public async Task<FormSubmissionResult> SubmitFormAsync(
		Guid tenantId,
		Guid pathwayId,
		Guid formId,
		Dictionary<string, string> responses)
	{
		var pathway = await db.PatientPathways.FindAsync(pathwayId);
		var formDef = await db.FormDefinitions.FindAsync(formId);

		// 1. VALIDATE responses against form schema
		ValidateFormResponses(responses, formDef);

		// 2. STORE the responses
		pathway.PathwayData["formResponses"][formId] = responses;
		await db.SaveChangesAsync();

		// 3. RECORD event
		var @event = new WorkflowEvent
		{
			Id = Guid.NewGuid(),
			TenantId = tenantId,
			PatientPathwayId = pathwayId,
			EventType = "FormSubmitted",
			EventSource = "Manual",
			OccurredAt = DateTime.UtcNow,
			Data = JsonSerializer.Serialize(responses),
			Description = $"Patient submitted {formDef.Title}",
			VisibleToPatient = true  // Important event, show to patient
		};
		await db.WorkflowEvents.AddAsync(@event);

		// 4. EVALUATE conditions (conditional progression)
		var conditionsToApply = formDef.ConditionalLogic
			.Where(cond => MatchesCondition(responses, cond))
			.ToList();

		var result = new FormSubmissionResult
		{
			FormId = formId,
			Success = true,
			Events = new List<string>()
		};

		// 5. EXECUTE actions from conditions
		foreach (var condition in conditionsToApply)
		{
			if (condition.Action == "CreateTask")
			{
				var task = new TaskItem
				{
					Id = Guid.NewGuid(),
					TenantId = tenantId,
					PatientPathwayId = pathwayId,
					Title = condition.ActionParameters["taskTitle"],
					Description = condition.ActionParameters["taskDescription"],
					Type = condition.ActionParameters["taskType"],
					Status = "Pending",
					Priority = condition.ActionParameters.GetValueOrDefault("priority", "Normal"),
					CreatedAt = DateTime.UtcNow,
					DueDate = DateTime.UtcNow.AddDays(3),
					CreationSource = "FormResponse",
					AssignedToRole = condition.ActionParameters.GetValueOrDefault("assignTo", "Nurse")
				};

				await db.TaskItems.AddAsync(task);
				result.Events.Add($"Task created: {task.Title}");
			}

			if (condition.Action == "TransitionStage")
			{
				var nextStageOrder = int.Parse(condition.ActionParameters["nextStageOrder"]);
				var nextStage = await db.ProcessStages
					.FirstAsync(s => s.ProgramConfigurationId == pathway.ProgramConfigurationId
								   && s.Order == nextStageOrder);

				pathway.CurrentProcessStageId = nextStage.Id;
				pathway.LastTransitionAt = DateTime.UtcNow;

				// Record transition event
				await db.WorkflowEvents.AddAsync(new WorkflowEvent
				{
					Id = Guid.NewGuid(),
					TenantId = tenantId,
					PatientPathwayId = pathwayId,
					EventType = "StageTransitioned",
					EventSource = "Automatic",
					OccurredAt = DateTime.UtcNow,
					Description = $"Auto-transitioned to {nextStage.Name} based on form response",
					VisibleToPatient = true
				});

				result.Events.Add($"Transitioned to stage: {nextStage.Name}");
			}

			if (condition.Action == "SendCommunication")
			{
				var template = await db.ContentTemplates
					.FirstAsync(t => t.Id == Guid.Parse(condition.ActionParameters["templateId"]));

				var message = new CommunicationMessage
				{
					Id = Guid.NewGuid(),
					TenantId = tenantId,
					PatientId = pathway.PatientId,
					Channel = condition.ActionParameters["channel"],  // Email, SMS, etc.
					ContentTemplateId = template.Id,
					RenderedContent = RenderTemplate(template, pathway),
					Status = "Pending",
					CreatedAt = DateTime.UtcNow
				};

				await db.CommunicationMessages.AddAsync(message);
				result.Events.Add($"Communication queued: {message.Channel}");
			}
		}

		await db.SaveChangesAsync();
		return result;
	}

	private bool MatchesCondition(Dictionary<string, string> responses, ConditionalLogic condition)
	{
		// Example: {"field": "severity", "value": "high"}
		var field = condition.ConditionJson["field"].GetString();
		var expectedValue = condition.ConditionJson["value"].GetString();

		return responses.ContainsKey(field) && responses[field] == expectedValue;
	}
}
```

---

### Phase 4: Communication System (Design for Integration)

```csharp
// ABSTRACTION: Allows different communication channels
public interface ICommunicationProvider
{
	Task<CommunicationResult> SendAsync(CommunicationMessage message);
}

// IMPLEMENTATIONS (can be added incrementally)
public class EmailCommunicationProvider : ICommunicationProvider
{
	// Implement: Send via SendGrid, AWS SES, etc.
	public async Task<CommunicationResult> SendAsync(CommunicationMessage message)
	{
		// TODO: Integrate with email service
		throw new NotImplementedException("Email provider not yet integrated");
	}
}

public class SmsCommunicationProvider : ICommunicationProvider
{
	// Implement: Send via Twilio, AWS SNS, etc.
	public async Task<CommunicationResult> SendAsync(CommunicationMessage message)
	{
		// TODO: Integrate with SMS service
		throw new NotImplementedException("SMS provider not yet integrated");
	}
}

public class WhatsAppCommunicationProvider : ICommunicationProvider
{
	// Implement: Send via Twilio or Meta
	public async Task<CommunicationResult> SendAsync(CommunicationMessage message)
	{
		// TODO: Integrate with WhatsApp service
		throw new NotImplementedException("WhatsApp provider not yet integrated");
	}
}

public class VoiceCommunicationProvider : ICommunicationProvider
{
	// Implement: Send via Twilio Voice API
	public async Task<CommunicationResult> SendAsync(CommunicationMessage message)
	{
		// TODO: Integrate with voice service
		throw new NotImplementedException("Voice provider not yet integrated");
	}
}

// SERVICE: Orchestrates communication
public interface ICommunicationService
{
	Task<CommunicationResult> SendMessageAsync(CommunicationMessage message);
	Task ProcessPendingMessagesAsync(Guid tenantId);  // Background job
	Task<List<CommunicationMessage>> GetMessageHistoryAsync(Guid tenantId, Guid patientId);
}

public class CommunicationService : ICommunicationService
{
	public async Task<CommunicationResult> SendMessageAsync(CommunicationMessage message)
	{
		var provider = GetProviderForChannel(message.Channel);

		try
		{
			var result = await provider.SendAsync(message);

			message.Status = result.Success ? "Sent" : "Failed";
			message.SentAt = DateTime.UtcNow;
			message.DeliveryError = result.ErrorMessage;

			await db.SaveChangesAsync();
			return result;
		}
		catch (Exception ex)
		{
			message.Status = "Failed";
			message.DeliveryError = ex.Message;
			await db.SaveChangesAsync();
			throw;
		}
	}

	private ICommunicationProvider GetProviderForChannel(string channel)
	{
		return channel switch
		{
			"Email" => serviceProvider.GetRequiredService<EmailCommunicationProvider>(),
			"SMS" => serviceProvider.GetRequiredService<SmsCommunicationProvider>(),
			"WhatsApp" => serviceProvider.GetRequiredService<WhatsAppCommunicationProvider>(),
			"Voice" => serviceProvider.GetRequiredService<VoiceCommunicationProvider>(),
			_ => throw new ArgumentException($"Unknown channel: {channel}")
		};
	}
}
```

---

### Phase 5: Timeline API Endpoints

```csharp
[ApiController]
[Route("api/workflow")]
public class TimelineController : ControllerBase
{
	private readonly IWorkflowService _workflowService;
	private readonly IFormResponseService _formResponseService;
	private readonly ITimeBasedTriggerService _triggerService;

	// GET: Patient's timeline of events
	[HttpGet("patients/{patientId}/timeline")]
	public async Task<ActionResult<TimelineResponseDto>> GetPatientTimeline(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid patientId,
		[FromQuery] int limit = 50)
	{
		var events = await db.WorkflowEvents
			.Where(e => e.TenantId == tenantId
					 && e.PatientId == patientId
					 && e.VisibleToPatient)  // Only show patient-visible events
			.OrderByDescending(e => e.OccurredAt)
			.Take(limit)
			.ToListAsync();

		return Ok(new TimelineResponseDto
		{
			Events = events.Select(e => new EventDto
			{
				Id = e.Id,
				Type = e.EventType,
				Description = e.Description,
				OccurredAt = e.OccurredAt,
				Source = e.EventSource
			}).ToList()
		});
	}

	// GET: Patient's active tasks
	[HttpGet("patients/{patientId}/tasks")]
	public async Task<ActionResult<List<TaskDto>>> GetPatientTasks(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid patientId,
		[FromQuery] string status = "Pending")
	{
		var tasks = await db.TaskItems
			.Where(t => t.TenantId == tenantId
					 && t.PatientId == patientId
					 && t.Status == status)
			.OrderBy(t => t.DueDate)
			.ToListAsync();

		return Ok(tasks.Select(t => new TaskDto
		{
			Id = t.Id,
			Title = t.Title,
			Description = t.Description,
			Type = t.Type,
			Status = t.Status,
			Priority = t.Priority,
			DueDate = t.DueDate,
			AssignedToRole = t.AssignedToRole,
			CreationSource = t.CreationSource
		}).ToList());
	}

	// POST: Submit form with conditional logic
	[HttpPost("pathways/{pathwayId}/submit-form")]
	public async Task<ActionResult<FormSubmissionResultDto>> SubmitForm(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid pathwayId,
		[FromBody] FormSubmissionRequest request)
	{
		var result = await _formResponseService.SubmitFormAsync(
			tenantId, pathwayId, request.FormId, request.Responses);

		return Ok(new FormSubmissionResultDto
		{
			Success = result.Success,
			Events = result.Events,
			TriggeredActions = result.Events  // What automatically happened
		});
	}

	// GET: Questionnaire status for patient
	[HttpGet("pathways/{pathwayId}/questionnaires")]
	public async Task<ActionResult<List<QuestionnaireDto>>> GetPatientQuestionnaires(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid pathwayId)
	{
		var pathway = await db.PatientPathways.FindAsync(pathwayId);

		var questionnaires = await db.Questionnaires
			.Where(q => q.TenantId == tenantId
					 && q.ProgramConfigurationId == pathway.ProgramConfigurationId)
			.ToListAsync();

		var result = new List<QuestionnaireDto>();

		foreach (var q in questionnaires)
		{
			var latestResponse = await db.QuestionnaireResponses
				.Where(qr => qr.TenantId == tenantId
						  && qr.PatientPathwayId == pathwayId
						  && qr.QuestionnaireId == q.Id)
				.OrderByDescending(qr => qr.SentAt)
				.FirstOrDefaultAsync();

			result.Add(new QuestionnaireDto
			{
				Id = q.Id,
				Name = q.Name,
				Status = latestResponse?.Status ?? "NotSent",
				SentAt = latestResponse?.SentAt,
				CompletedAt = latestResponse?.CompletedAt,
				NextScheduledFor = latestResponse?.NextQuestionnaireScheduledFor
			});
		}

		return Ok(result);
	}
}
```

---

### Phase 6: Background Job for Triggers & Messages

```csharp
// Register in Program.cs
builder.Services.AddHostedService<WorkflowBackgroundJobService>();

public class WorkflowBackgroundJobService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly PeriodicTimer _timer;

	public WorkflowBackgroundJobService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_timer = new PeriodicTimer(TimeSpan.FromMinutes(1));  // Run every minute
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using (_timer)
		{
			while (await _timer.WaitForNextTickAsync(stoppingToken))
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var triggerService = scope.ServiceProvider.GetRequiredService<ITimeBasedTriggerService>();
					var communicationService = scope.ServiceProvider.GetRequiredService<ICommunicationService>();

					// Get all tenants that need processing
					var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
					var tenants = await db.Tenants.Select(t => t.Id).Distinct().ToListAsync();

					foreach (var tenantId in tenants)
					{
						// Process time-based triggers (e.g., 7 days after enrollment)
						await triggerService.ProcessPendingTriggersAsync(tenantId);

						// Send pending messages (emails, SMS, etc.)
						await communicationService.ProcessPendingMessagesAsync(tenantId);
					}
				}
				catch (Exception ex)
				{
					// Log error but don't stop the job
					Console.WriteLine($"Error in WorkflowBackgroundJobService: {ex}");
				}
			}
		}
	}
}
```

---

## Complete Flow: Timeline Example

```
PATIENT JOURNEY WITH NEW ENTITIES:

T=01/01 10:00 AM
├─ EVENT: Lead Created
│  └─ WorkflowEvent created (invisible to patient)
│  └─ TriggerExecution: Check "OnLeadCreated" rules
│
├─ ACTION: Send Consent via Email
│  └─ CommunicationMessage created
│  └─ QUEUE for sending
│
└─ TASK: [SYSTEM] Consent Email Sent
   └─ TaskItem created (source: "Automatic")
   └─ VisibleToPatient: true

T=01/02 02:30 PM
├─ EVENT: Consent Signed (patient clicks link)
│  └─ WorkflowEvent created (visible to patient)
│
├─ TRIGGER: Event-based rule fires
│  └─ TriggerExecution records: "OnConsentSigned" fired
│
├─ ACTION: Auto-advance to Enrollment stage
│  └─ Pathway.CurrentProcessStageId updated
│  └─ WorkflowEvent: "StageTransitioned"
│
└─ ACTION: Create "Insurance Verification" task for Admin
   └─ TaskItem created (assigned to: Admin role)

T=01/03 09:00 AM
├─ EVENT: Patient Enrolled
│  └─ WorkflowEvent created
│  └─ Enrollment persisted (existing entity)
│
└─ ACTION: Assign Nurse + Schedule Follow-up
   ├─ TASK: Nurse review (AssignedToRole: "Nurse")
   └─ TRIGGER: 7-day timer starts

T=01/10 (7 days later)
├─ TIME-BASED TRIGGER: "7DaysAfterEnrollment"
│  └─ Check: Has 7 days passed? YES
│  └─ Check: Already triggered today? NO
│  └─ TriggerExecution record created
│
├─ ACTION: Create Follow-up Task
│  └─ TaskItem created
│  └─ AssignedToUserId: {nurse_id}
│  └─ DueDate: 01/12
│  └─ Priority: "High"
│
├─ ACTION: Send Reminder via SMS
│  └─ CommunicationMessage created
│  └─ Channel: "SMS"
│  └─ Recipient: patient phone
│
└─ EVENT: Follow-up Scheduled
   └─ WorkflowEvent created (visible to patient)

T=01/10 03:00 PM
├─ TASK: Nurse completes phone call
│  └─ TaskItem.Status = "Completed"
│  └─ TaskItem.CompletedAt = now
│  └─ TaskItem.Outcome = "Completed"
│
├─ EVENT: Task Completed
│  └─ WorkflowEvent created
│
└─ TRIGGER: Event-based rule "OnNurseCallCompleted"
   ├─ Check condition: "Patient has high-risk score?"
   ├─ IF YES → Create urgent task for doctor
   └─ IF NO → Schedule next follow-up

T=01/20
├─ QUESTIONNAIRE TRIGGER: "30DaysAfterEnrollment"
│  └─ Questionnaire scheduled: "Health Assessment Form"
│
├─ ACTION: Send Questionnaire via Email
│  └─ QuestionnaireResponse created (status: "Sent")
│  └─ CommunicationMessage created and sent
│
└─ TIMER: 7-day timeout for completion

T=01/21
├─ EVENT: Patient Opens Email
│  └─ WorkflowEvent: "QuestionnaireOpened"
│
├─ PATIENT SUBMITS QUESTIONNAIRE
│  └─ QuestionnaireResponse.Status = "Completed"
│  └─ Responses stored with timestamps
│
├─ CONDITION EVALUATION
│  └─ Check: "If answer to Q5 = 'severe', create urgent task"
│  └─ Match found!
│
├─ AUTO-ACTIONS TRIGGERED
│  ├─ Create urgent task: "Doctor review required"
│  ├─ Send email: "We received your questionnaire, doctor reviews soon"
│  └─ Schedule next questionnaire: 30 days from now
│
└─ TIMELINE UPDATED
   └─ All events visible to patient for review
   └─ Nurse can see all tasks and history
   └─ Admin can see full audit trail
```

---

## Summary: What Gets Added

### New Entities
```
✅ WorkflowEvent          - Timeline of all events
✅ Questionnaire          - Form templates with scheduling
✅ QuestionnaireResponse  - Patient's questionnaire answers
✅ TriggerExecution       - Track when triggers run, retry, fail
✅ CommunicationMessage   - Messages sent via channels
✅ TaskItem (EXTENDED)    - Full task properties
```

### New Services
```
✅ TimeBasedTriggerService    - Process time-based triggers
✅ EventBasedTriggerService   - Process event-based triggers
✅ FormResponseService        - Handle conditional form logic
✅ CommunicationService       - Orchestrate message sending
✅ WorkflowBackgroundJobService - Background processing
```

### New Endpoints
```
✅ GET  /api/workflow/patients/{id}/timeline
✅ GET  /api/workflow/patients/{id}/tasks
✅ POST /api/workflow/pathways/{id}/submit-form
✅ GET  /api/workflow/pathways/{id}/questionnaires
```

### Key Features Demonstrated
```
✅ Timeline of patient journey
✅ Time-based automation (7 days after enrollment)
✅ Event-based automation (on form submit)
✅ Conditional logic (if response = X, do Y)
✅ Task management with full properties
✅ Questionnaire scheduling and tracking
✅ Retry/duplicate/failure handling
✅ Communication system abstraction (SMS, Email, WhatsApp, Voice ready)
✅ Background job processing
```

**This elevates the implementation from a simple workflow to a complete patient care platform.**
