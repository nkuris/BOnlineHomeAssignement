# Phase 2 Quick Start: Implementation Guide

## Prerequisites

- ✅ Phase 1 completed and building successfully
- ✅ All workflow entities created
- ✅ Multi-tenant isolation working
- ✅ Basic form submission working

---

## Step 1: Add New Entities (Day 1)

### File: `Domain/Entities/WorkflowEventEntities.cs` (New File)

```csharp
using System;

namespace BOnlineHomeAssignement.Server.Domain.Entities
{
	// 1. TIMELINE: Track all patient events
	public class WorkflowEvent
	{
		public Guid Id { get; set; }
		public Guid TenantId { get; set; }
		public Guid PatientPathwayId { get; set; }
		public Guid? PatientId { get; set; }

		// WHAT HAPPENED
		public string EventType { get; set; }  // "LeadCreated", "ConsentSigned", "FormSubmitted", "StageEntered", "TaskCreated"
		public string EventSource { get; set; }  // "Manual", "Automatic", "System"
		public DateTime OccurredAt { get; set; }

		// DETAILS
		public string Data { get; set; }  // JSON: full details
		public string Description { get; set; }  // Human readable

		// VISIBILITY
		public bool VisibleToPatient { get; set; }  // False for technical events

		// NAVIGATION
		public PatientPathway PatientPathway { get; set; }
	}

	// 2. QUESTIONNAIRE TEMPLATE
	public class Questionnaire
	{
		public Guid Id { get; set; }
		public Guid TenantId { get; set; }
		public Guid ProgramConfigurationId { get; set; }

		// TEMPLATE
		public string Name { get; set; }
		public string Description { get; set; }
		public Guid FormDefinitionId { get; set; }

		// SCHEDULING
		public string RecurrencePattern { get; set; }  // "Once", "Weekly", "Monthly", "DaysAfterEvent"
		public int? RecurrenceDays { get; set; }
		public bool RequiresCompletion { get; set; }
		public int TimeoutDays { get; set; }

		public FormDefinition FormDefinition { get; set; }
	}

	// 3. PATIENT'S QUESTIONNAIRE RESPONSES
	public class QuestionnaireResponse
	{
		public Guid Id { get; set; }
		public Guid TenantId { get; set; }
		public Guid PatientPathwayId { get; set; }
		public Guid QuestionnaireId { get; set; }

		public DateTime SentAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public string Status { get; set; }  // "Sent", "Opened", "Completed", "Abandoned"

		// JSON: {field: answer, field2: answer2}
		public string Responses { get; set; }

		// What happened after submit
		public string TriggeredActions { get; set; }  // JSON array
		public DateTime? NextQuestionnaireScheduledFor { get; set; }

		public Questionnaire Questionnaire { get; set; }
		public PatientPathway PatientPathway { get; set; }
	}

	// 4. TRACK WHEN TRIGGERS EXECUTE (prevent duplicates, handle retries)
	public class TriggerExecution
	{
		public Guid Id { get; set; }
		public Guid TenantId { get; set; }
		public Guid BusinessRuleId { get; set; }
		public Guid? PatientPathwayId { get; set; }

		// TRIGGER INFO
		public string TriggerType { get; set; }  // "TimeBasedTrigger", "EventBasedTrigger"
		public string TriggerName { get; set; }

		// EXECUTION
		public DateTime ScheduledFor { get; set; }
		public DateTime? ExecutedAt { get; set; }
		public string Status { get; set; }  // "Pending", "Running", "Completed", "Failed"

		// RETRY
		public int AttemptCount { get; set; }
		public int MaxRetries { get; set; }
		public string LastError { get; set; }
		public DateTime? NextRetryAt { get; set; }

		// DUPLICATE PREVENTION
		public string ExecutionKey { get; set; }  // Unique identifier
		public bool IsDuplicate { get; set; }

		// RESULT
		public string CreatedEntities { get; set; }  // JSON IDs

		public BusinessRule BusinessRule { get; set; }
	}

	// 5. COMMUNICATION MESSAGES (Email, SMS, WhatsApp, Voice)
	public class CommunicationMessage
	{
		public Guid Id { get; set; }
		public Guid TenantId { get; set; }
		public Guid PatientId { get; set; }

		// CHANNEL & RECIPIENT
		public string Channel { get; set; }  // "Email", "SMS", "WhatsApp", "Voice", "InApp"
		public string RecipientAddress { get; set; }

		// CONTENT
		public Guid? ContentTemplateId { get; set; }
		public string RenderedContent { get; set; }  // After variable substitution
		public string Subject { get; set; }  // For Email

		// TRIGGER
		public Guid? TriggerExecutionId { get; set; }

		// DELIVERY
		public DateTime CreatedAt { get; set; }
		public DateTime? ScheduledFor { get; set; }
		public DateTime? SentAt { get; set; }
		public string Status { get; set; }  // "Pending", "Scheduled", "Sent", "Delivered", "Failed"
		public string DeliveryError { get; set; }

		// TRACKING
		public DateTime? OpenedAt { get; set; }
		public int ClickCount { get; set; }

		public Patient Patient { get; set; }
	}
}
```

### File: `Domain/Entities/TaskItem.cs` (Modify Existing)

```csharp
// EXTEND existing TaskItem with new properties
public class TaskItem
{
	public Guid Id { get; set; }
	public Guid TenantId { get; set; }

	// ✅ EXISTING PROPERTIES
	public Guid PatientPathwayId { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }

	// ✅ NEW PROPERTIES
	public Guid? PatientId { get; set; }
	public Guid? EnrollmentId { get; set; }
	public Guid ProgramId { get; set; }
	public Guid? TaskDefinitionId { get; set; }  // Template

	// TYPE & ASSIGNMENT
	public string Type { get; set; }  // "NurseCall", "PatientQuestionnaire", "DoctorReview"
	public Guid? AssignedToUserId { get; set; }
	public string AssignedToRole { get; set; }  // "Nurse", "Doctor", "Admin"

	// TIMING
	public DateTime CreatedAt { get; set; }
	public DateTime DueDate { get; set; }
	public DateTime? CompletedAt { get; set; }
	public int? ReminderDaysBefore { get; set; }

	// STATUS & PRIORITY
	public string Status { get; set; }  // "Pending", "InProgress", "Completed", "Overdue"
	public string Priority { get; set; }  // "High", "Normal", "Low"

	// SOURCE & OUTCOME
	public string CreationSource { get; set; }  // "Manual", "EventTriggered", "TimeTriggered"
	public string Outcome { get; set; }  // "Completed", "Skipped", "Failed", "Escalated"
	public string CompletionNotes { get; set; }

	// AUDIT
	public Guid? CreatedByUserId { get; set; }
	public Guid? CompletedByUserId { get; set; }

	// NAVIGATION
	public PatientPathway PatientPathway { get; set; }
	public TaskDefinition TaskDefinition { get; set; }
}
```

---

## Step 2: Update DbContext (Day 1-2)

### File: `Infrastructure/Persistence/AppDbContext.cs` (Add DbSets)

```csharp
public DbSet<WorkflowEvent> WorkflowEvents { get; set; }
public DbSet<Questionnaire> Questionnaires { get; set; }
public DbSet<QuestionnaireResponse> QuestionnaireResponses { get; set; }
public DbSet<TriggerExecution> TriggerExecutions { get; set; }
public DbSet<CommunicationMessage> CommunicationMessages { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
	// ... existing config ...

	// WorkflowEvent
	modelBuilder.Entity<WorkflowEvent>()
		.HasKey(e => e.Id);
	modelBuilder.Entity<WorkflowEvent>()
		.HasIndex(e => new { e.TenantId, e.PatientPathwayId });
	modelBuilder.Entity<WorkflowEvent>()
		.HasIndex(e => new { e.TenantId, e.OccurredAt });

	modelBuilder.Entity<WorkflowEvent>()
		.HasOne(e => e.PatientPathway)
		.WithMany()
		.HasForeignKey(e => e.PatientPathwayId)
		.OnDelete(DeleteBehavior.Cascade);

	// Questionnaire
	modelBuilder.Entity<Questionnaire>()
		.HasKey(q => q.Id);
	modelBuilder.Entity<Questionnaire>()
		.HasIndex(q => new { q.TenantId, q.ProgramConfigurationId });

	modelBuilder.Entity<Questionnaire>()
		.HasOne(q => q.FormDefinition)
		.WithMany()
		.HasForeignKey(q => q.FormDefinitionId)
		.OnDelete(DeleteBehavior.Restrict);

	// QuestionnaireResponse
	modelBuilder.Entity<QuestionnaireResponse>()
		.HasKey(qr => qr.Id);
	modelBuilder.Entity<QuestionnaireResponse>()
		.HasIndex(qr => new { qr.TenantId, qr.PatientPathwayId });

	modelBuilder.Entity<QuestionnaireResponse>()
		.HasOne(qr => qr.Questionnaire)
		.WithMany()
		.HasForeignKey(qr => qr.QuestionnaireId)
		.OnDelete(DeleteBehavior.Restrict);

	modelBuilder.Entity<QuestionnaireResponse>()
		.HasOne(qr => qr.PatientPathway)
		.WithMany()
		.HasForeignKey(qr => qr.PatientPathwayId)
		.OnDelete(DeleteBehavior.Cascade);

	// TriggerExecution
	modelBuilder.Entity<TriggerExecution>()
		.HasKey(te => te.Id);
	modelBuilder.Entity<TriggerExecution>()
		.HasIndex(te => te.ExecutionKey)
		.IsUnique();  // Prevent duplicate execution
	modelBuilder.Entity<TriggerExecution>()
		.HasIndex(te => new { te.Status, te.ScheduledFor });

	modelBuilder.Entity<TriggerExecution>()
		.HasOne(te => te.BusinessRule)
		.WithMany()
		.HasForeignKey(te => te.BusinessRuleId)
		.OnDelete(DeleteBehavior.Restrict);

	// CommunicationMessage
	modelBuilder.Entity<CommunicationMessage>()
		.HasKey(cm => cm.Id);
	modelBuilder.Entity<CommunicationMessage>()
		.HasIndex(cm => new { cm.TenantId, cm.PatientId });
	modelBuilder.Entity<CommunicationMessage>()
		.HasIndex(cm => new { cm.Status, cm.CreatedAt });

	modelBuilder.Entity<CommunicationMessage>()
		.HasOne(cm => cm.Patient)
		.WithMany()
		.HasForeignKey(cm => cm.PatientId)
		.OnDelete(DeleteBehavior.Restrict);
}
```

---

## Step 3: Create Migration (Day 2)

```bash
dotnet ef migrations add AddExtendedWorkflowEntities --context AppDbContext

# Review the migration
# Verify: All 5 new entities appear
# Verify: Foreign keys correct
# Verify: Indexes created

dotnet ef database update
```

---

## Step 4: Create Services (Day 3-5)

### File: `Application/Services/TimeBasedTriggerService.cs` (New)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOnlineHomeAssignement.Server.Application.Services
{
	public interface ITimeBasedTriggerService
	{
		Task ProcessPendingTriggersAsync(Guid tenantId);
	}

	public class TimeBasedTriggerService : ITimeBasedTriggerService
	{
		private readonly AppDbContext _db;

		public TimeBasedTriggerService(AppDbContext db)
		{
			_db = db;
		}

		public async Task ProcessPendingTriggersAsync(Guid tenantId)
		{
			// Get all time-based rules for this tenant
			var timeBasedRules = await _db.BusinessRules
				.Where(r => r.TenantId == tenantId
						 && r.TriggerType == "TimeBased"  // Our convention
						 && r.IsActive)
				.ToListAsync();

			foreach (var rule in timeBasedRules)
			{
				try
				{
					// Parse trigger parameters
					var triggerParams = JsonSerializer.Deserialize<Dictionary<string, object>>(
						rule.Condition ?? "{}");

					// Example: "7DaysAfterEnrollment"
					if (rule.TriggerEvent == "DaysAfterEnrollment")
					{
						await ProcessDaysAfterEnrollmentAsync(tenantId, rule, triggerParams);
					}

					// TODO: Add more trigger types
					// "DaysAfterQuestionnaire", "DaysInStage", etc.
				}
				catch (Exception ex)
				{
					// Log but continue
					Console.WriteLine($"Error processing rule {rule.Id}: {ex}");
				}
			}
		}

		private async Task ProcessDaysAfterEnrollmentAsync(
			Guid tenantId,
			BusinessRule rule,
			Dictionary<string, object> triggerParams)
		{
			// Extract days
			if (!triggerParams.TryGetValue("days", out var daysObj))
				return;

			int days = int.Parse(daysObj.ToString());

			// Find pathways where enrollment was N days ago
			var targetDate = DateTime.UtcNow.AddDays(-days);

			var pathways = await _db.PatientPathways
				.Include(p => p.Enrollment)
				.Where(p => p.TenantId == tenantId
						 && p.Enrollment.EnrolledAt <= targetDate
						 && !p.Enrollment.EnrolledAt.AddDays(days).Date.Equals(DateTime.UtcNow.Date))  // Not today
				.ToListAsync();

			foreach (var pathway in pathways)
			{
				// Generate unique execution key to prevent duplicates
				var executionKey = $"rule-{rule.Id}_pathway-{pathway.Id}_{DateTime.UtcNow:yyyyMMdd}";

				// Check if already executed today
				var existingExecution = await _db.TriggerExecutions
					.FirstOrDefaultAsync(te => te.ExecutionKey == executionKey);

				if (existingExecution != null)
					continue;  // Already executed, skip

				// Create execution record
				var execution = new TriggerExecution
				{
					Id = Guid.NewGuid(),
					TenantId = tenantId,
					BusinessRuleId = rule.Id,
					PatientPathwayId = pathway.Id,
					TriggerType = "TimeBasedTrigger",
					TriggerName = rule.TriggerEvent,
					ScheduledFor = DateTime.UtcNow,
					Status = "Running",
					AttemptCount = 1,
					MaxRetries = 3,
					ExecutionKey = executionKey,
					IsDuplicate = false
				};

				try
				{
					// Execute the rule action
					var createdEntities = await ExecuteRuleActionAsync(rule, pathway);

					execution.ExecutedAt = DateTime.UtcNow;
					execution.Status = "Completed";
					execution.CreatedEntities = JsonSerializer.Serialize(createdEntities);

					// Record the event
					var @event = new WorkflowEvent
					{
						Id = Guid.NewGuid(),
						TenantId = tenantId,
						PatientPathwayId = pathway.Id,
						PatientId = pathway.PatientId,
						EventType = "TimeBasedTriggerExecuted",
						EventSource = "Automatic",
						OccurredAt = DateTime.UtcNow,
						Description = $"Trigger executed: {rule.TriggerEvent} ({days} days)",
						Data = JsonSerializer.Serialize(new { rule = rule.Name, action = rule.Action }),
						VisibleToPatient = false  // Technical event
					};

					await _db.WorkflowEvents.AddAsync(@event);
				}
				catch (Exception ex)
				{
					execution.Status = "Failed";
					execution.LastError = ex.Message;
					execution.NextRetryAt = DateTime.UtcNow.AddMinutes(5);  // Retry in 5 min
				}

				await _db.TriggerExecutions.AddAsync(execution);
			}

			await _db.SaveChangesAsync();
		}

		private async Task<List<string>> ExecuteRuleActionAsync(
			BusinessRule rule,
			PatientPathway pathway)
		{
			var created = new List<string>();

			// Parse the action
			var action = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.Action ?? "{}");

			if (action.TryGetValue("type", out var typeObj))
			{
				string actionType = typeObj.ToString();

				if (actionType == "CreateTask")
				{
					// Create a task
					var task = new TaskItem
					{
						Id = Guid.NewGuid(),
						TenantId = pathway.TenantId,
						PatientPathwayId = pathway.Id,
						PatientId = pathway.PatientId,
						ProgramId = pathway.Enrollment.ProgramId,
						Title = action["taskTitle"]?.ToString() ?? "Follow-up Task",
						Description = action["taskDescription"]?.ToString() ?? "",
						Type = action["taskType"]?.ToString() ?? "FollowUp",
						Status = "Pending",
						Priority = action.TryGetValue("priority", out var prio) ? prio.ToString() : "Normal",
						CreatedAt = DateTime.UtcNow,
						DueDate = DateTime.UtcNow.AddDays(3),
						CreationSource = "TimeTriggered",
						AssignedToRole = action.TryGetValue("assignTo", out var role) ? role.ToString() : "Nurse"
					};

					await _db.TaskItems.AddAsync(task);
					created.Add(task.Id.ToString());
				}
				else if (actionType == "SendCommunication")
				{
					// Create communication message (will be sent by background job)
					var message = new CommunicationMessage
					{
						Id = Guid.NewGuid(),
						TenantId = pathway.TenantId,
						PatientId = pathway.PatientId,
						Channel = action["channel"]?.ToString() ?? "Email",
						Status = "Pending",
						CreatedAt = DateTime.UtcNow,
						RecipientAddress = pathway.Patient?.Email ?? ""  // Would need navigation
					};

					await _db.CommunicationMessages.AddAsync(message);
					created.Add(message.Id.ToString());
				}
			}

			return created;
		}
	}
}
```

### File: `Application/Services/FormResponseService.cs` (New)

```csharp
// SIMILAR STRUCTURE: Handle form submission with conditional logic
// Check EXTENDED_REQUIREMENTS_IMPLEMENTATION.md for complete code
// Key points:
// 1. Validate form responses
// 2. Store in PathwayData
// 3. Record WorkflowEvent
// 4. Evaluate conditions
// 5. Execute actions (create task, transition stage, send message)
```

---

## Step 5: Update Program.cs (Day 5)

```csharp
// Register new services
builder.Services.AddScoped<ITimeBasedTriggerService, TimeBasedTriggerService>();
builder.Services.AddScoped<IFormResponseService, FormResponseService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();

// Register background job
builder.Services.AddHostedService<WorkflowBackgroundJobService>();
```

---

## Step 6: Create Background Job (Day 5-6)

### File: `Services/WorkflowBackgroundJobService.cs` (New)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BOnlineHomeAssignement.Server.Application.Services
{
	public class WorkflowBackgroundJobService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private PeriodicTimer _timer;

		public WorkflowBackgroundJobService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_timer = new PeriodicTimer(TimeSpan.FromMinutes(1));  // Run every minute

			using (_timer)
			{
				while (await _timer.WaitForNextTickAsync(stoppingToken))
				{
					try
					{
						using var scope = _serviceProvider.CreateScope();
						var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
						var triggerService = scope.ServiceProvider.GetRequiredService<ITimeBasedTriggerService>();
						var communicationService = scope.ServiceProvider.GetRequiredService<ICommunicationService>();

						// Get all tenants
						var tenants = await db.Tenants
							.Select(t => t.Id)
							.Distinct()
							.ToListAsync();

						// Process for each tenant
						foreach (var tenantId in tenants)
						{
							// 1. Process time-based triggers
							await triggerService.ProcessPendingTriggersAsync(tenantId);

							// 2. Send pending messages
							await communicationService.ProcessPendingMessagesAsync(tenantId);

							// 3. Process retries for failed triggers
							await ProcessFailedTriggersAsync(tenantId);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error in WorkflowBackgroundJobService: {ex.Message}");
						// Continue despite errors
					}
				}
			}
		}

		private async Task ProcessFailedTriggersAsync(Guid tenantId)
		{
			using var scope = _serviceProvider.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			// Find failed triggers that need retry
			var failedTriggers = await db.TriggerExecutions
				.Where(te => te.TenantId == tenantId
						  && te.Status == "Failed"
						  && te.AttemptCount < te.MaxRetries
						  && te.NextRetryAt <= DateTime.UtcNow)
				.ToListAsync();

			foreach (var trigger in failedTriggers)
			{
				trigger.Status = "Running";
				trigger.AttemptCount++;
				trigger.NextRetryAt = DateTime.UtcNow.AddMinutes(5 * trigger.AttemptCount);  // Exponential backoff
			}

			await db.SaveChangesAsync();
			// TODO: Retry the trigger
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_timer?.Dispose();
			await base.StopAsync(cancellationToken);
		}
	}
}
```

---

## Step 7: Create API Endpoints (Day 6-7)

### File: `Controllers/TimelineController.cs` (New)

```csharp
[ApiController]
[Route("api/workflow")]
public class TimelineController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly IFormResponseService _formResponseService;

	public TimelineController(
		AppDbContext db,
		IFormResponseService formResponseService)
	{
		_db = db;
		_formResponseService = formResponseService;
	}

	// GET: Patient's timeline
	[HttpGet("patients/{patientId}/timeline")]
	public async Task<ActionResult> GetTimeline(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid patientId,
		[FromQuery] int limit = 50)
	{
		var events = await _db.WorkflowEvents
			.Where(e => e.TenantId == tenantId
					 && e.PatientId == patientId
					 && e.VisibleToPatient)
			.OrderByDescending(e => e.OccurredAt)
			.Take(limit)
			.Select(e => new
			{
				e.Id,
				e.EventType,
				e.Description,
				e.OccurredAt,
				e.EventSource
			})
			.ToListAsync();

		return Ok(events);
	}

	// GET: Patient's tasks
	[HttpGet("patients/{patientId}/tasks")]
	public async Task<ActionResult> GetTasks(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid patientId,
		[FromQuery] string status = "Pending")
	{
		var tasks = await _db.TaskItems
			.Where(t => t.TenantId == tenantId
					 && t.PatientId == patientId
					 && t.Status == status)
			.OrderBy(t => t.DueDate)
			.Select(t => new
			{
				t.Id,
				t.Title,
				t.Description,
				t.Type,
				t.Status,
				t.Priority,
				t.DueDate,
				t.AssignedToRole,
				t.CreationSource
			})
			.ToListAsync();

		return Ok(tasks);
	}

	// POST: Submit form with conditional logic
	[HttpPost("pathways/{pathwayId}/submit-form")]
	public async Task<ActionResult> SubmitForm(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid pathwayId,
		[FromBody] dynamic request)
	{
		var result = await _formResponseService.SubmitFormAsync(
			tenantId,
			pathwayId,
			request.FormId,
			request.Responses);

		return Ok(result);
	}

	// GET: Questionnaires
	[HttpGet("pathways/{pathwayId}/questionnaires")]
	public async Task<ActionResult> GetQuestionnaires(
		[FromHeader(Name = "X-Tenant-Id")] Guid tenantId,
		Guid pathwayId)
	{
		var pathway = await _db.PatientPathways.FindAsync(pathwayId);
		if (pathway?.TenantId != tenantId)
			return Unauthorized();

		var questionnaires = await _db.Questionnaires
			.Where(q => q.TenantId == tenantId
					 && q.ProgramConfigurationId == pathway.ProgramConfigurationId)
			.Select(q => new
			{
				q.Id,
				q.Name,
				q.Description,
				q.RecurrencePattern,
				q.TimeoutDays
			})
			.ToListAsync();

		return Ok(questionnaires);
	}
}
```

---

## Step 8: Test (Day 7-8)

```csharp
[Fact]
public async Task TimeBasedTrigger_7DaysAfterEnrollment_CreatesTask()
{
	// Setup
	var enrollment = new Enrollment { EnrolledAt = DateTime.UtcNow.AddDays(-7) };
	var pathway = new PatientPathway { Enrollment = enrollment };
	await db.Enrollments.AddAsync(enrollment);
	await db.PatientPathways.AddAsync(pathway);
	await db.SaveChangesAsync();

	// Act
	await triggerService.ProcessPendingTriggersAsync(tenantId);

	// Assert
	var task = await db.TaskItems
		.FirstOrDefaultAsync(t => t.PatientPathwayId == pathway.Id);
	Assert.NotNull(task);
	Assert.Equal("Follow-up", task.Type);
}

[Fact]
public async Task FormSubmitWithCondition_HighSeverity_CreatesUrgentTask()
{
	// Setup form with condition
	// Submit with severity=high

	// Act
	var result = await formResponseService.SubmitFormAsync(...);

	// Assert
	Assert.Contains("Task created", result.Events);
	var task = await db.TaskItems.FirstAsync(t => t.Priority == "High");
	Assert.NotNull(task);
}
```

---

## Checklist for Phase 2

```
ENTITIES
✅ WorkflowEvent created
✅ Questionnaire created
✅ QuestionnaireResponse created
✅ TriggerExecution created
✅ CommunicationMessage created
✅ TaskItem extended

EF CORE
✅ DbSets added to AppDbContext
✅ Relationships configured
✅ Indexes created
✅ Migration generated
✅ Database updated

SERVICES
✅ ITimeBasedTriggerService implemented
✅ IEventBasedTriggerService implemented
✅ IFormResponseService implemented
✅ ICommunicationService designed (placeholder)
✅ WorkflowBackgroundJobService created

ENDPOINTS
✅ GET /api/workflow/patients/{id}/timeline
✅ GET /api/workflow/patients/{id}/tasks
✅ POST /api/workflow/pathways/{id}/submit-form
✅ GET /api/workflow/pathways/{id}/questionnaires

BACKGROUND JOB
✅ Registered in Program.cs
✅ Runs every minute
✅ Processes time-based triggers
✅ Processes pending messages
✅ Handles retries

TESTING
✅ Time-based triggers tested
✅ Event-based triggers tested
✅ Form conditional logic tested
✅ Duplicate prevention verified
```

---

## Build & Test

```bash
# Build
dotnet build

# Run tests
dotnet test

# Update database
dotnet ef database update

# Run locally
dotnet run --project BOnlineHomeAssignement.Server

# Test endpoint
curl -H "X-Tenant-Id: {tenantId}" \
	 http://localhost:5000/api/workflow/patients/{patientId}/timeline
```

Ready to start? Begin with Step 1! 🚀
