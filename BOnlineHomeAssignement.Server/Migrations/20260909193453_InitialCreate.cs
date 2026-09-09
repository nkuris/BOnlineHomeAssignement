using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOnlineHomeAssignement.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntervalDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LeadSource = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryDocId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedNurseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CareTeamIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Hostname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollments_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProgramConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkflowDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultBusinessRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramConfigurations_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppointmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    SourceLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BusinessRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessRules_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportedVariables = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTemplates_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldsDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicableStages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisibilityConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnSubmitActions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormDefinitions_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcessStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationDays = table.Column<int>(type: "int", nullable: true),
                    AllowedStatuses = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredFormIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoAdvanceConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessStages_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Questionnaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldsDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecurrenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecurrenceValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggerEventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaxSendCount = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questionnaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questionnaires_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessibleStages = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Permissions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignableTaskTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanViewPathways = table.Column<bool>(type: "bit", nullable: false),
                    CanApproveTransitions = table.Column<bool>(type: "bit", nullable: false),
                    CanOverrideRules = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignToRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OffsetDays = table.Column<int>(type: "int", nullable: false),
                    DueDays = table.Column<int>(type: "int", nullable: true),
                    AutoEscalate = table.Column<bool>(type: "bit", nullable: false),
                    ContentTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDefinitions_ContentTemplates_ContentTemplateId",
                        column: x => x.ContentTemplateId,
                        principalTable: "ContentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaskDefinitions_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatientPathways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentProcessStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PatientType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PathwayData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnteredStageAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StageDueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTransitionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransitionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientPathways", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientPathways_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientPathways_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientPathways_ProcessStages_CurrentProcessStageId",
                        column: x => x.CurrentProcessStageId,
                        principalTable: "ProcessStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientPathways_ProgramConfigurations_ProgramConfigurationId",
                        column: x => x.ProgramConfigurationId,
                        principalTable: "ProgramConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedBySourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskItems_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaskItems_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskItems_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaskItems_TaskDefinitions_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "TaskDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunicationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientPathwayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MessageBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationMessages_PatientPathways_PatientPathwayId",
                        column: x => x.PatientPathwayId,
                        principalTable: "PatientPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientPathwayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireResponses_PatientPathways_PatientPathwayId",
                        column: x => x.PatientPathwayId,
                        principalTable: "PatientPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionnaireResponses_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TriggerExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientPathwayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriggerConfigId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ExecutionResult = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriggerExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TriggerExecutions_PatientPathways_PatientPathwayId",
                        column: x => x.PatientPathwayId,
                        principalTable: "PatientPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientPathwayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisibleToPatient = table.Column<bool>(type: "bit", nullable: false),
                    EventOccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowEvents_PatientPathways_PatientPathwayId",
                        column: x => x.PatientPathwayId,
                        principalTable: "PatientPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_EnrollmentId",
                table: "Appointments",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_ScheduledStart",
                table: "Appointments",
                column: "ScheduledStart");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_Tenant_Enrollment",
                table: "Appointments",
                columns: new[] { "TenantId", "EnrollmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointment_Tenant_Patient",
                table: "Appointments",
                columns: new[] { "TenantId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRule_Trigger",
                table: "BusinessRules",
                columns: new[] { "TenantId", "TriggerEvent", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_ProgramConfigurationId",
                table: "BusinessRules",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessage_Channel",
                table: "CommunicationMessages",
                columns: new[] { "TenantId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessage_Pathway",
                table: "CommunicationMessages",
                columns: new[] { "TenantId", "PatientPathwayId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessage_Pending",
                table: "CommunicationMessages",
                columns: new[] { "TenantId", "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_PatientPathwayId",
                table: "CommunicationMessages",
                column: "PatientPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTemplate_Type",
                table: "ContentTemplates",
                columns: new[] { "TenantId", "ProgramConfigurationId", "ContentType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTemplates_ProgramConfigurationId",
                table: "ContentTemplates",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PatientId",
                table: "Enrollments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ProgramId",
                table: "Enrollments",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_FormDef_Program",
                table: "FormDefinitions",
                columns: new[] { "TenantId", "ProgramConfigurationId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormDefinitions_ProgramConfigurationId",
                table: "FormDefinitions",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_Email",
                table: "Leads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_Tenant_Email",
                table: "Leads",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathway_Enrollment",
                table: "PatientPathways",
                columns: new[] { "TenantId", "EnrollmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathway_Stage",
                table: "PatientPathways",
                columns: new[] { "TenantId", "CurrentProcessStageId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathway_Tenant_Patient",
                table: "PatientPathways",
                columns: new[] { "TenantId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathways_CurrentProcessStageId",
                table: "PatientPathways",
                column: "CurrentProcessStageId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathways_EnrollmentId",
                table: "PatientPathways",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathways_PatientId",
                table: "PatientPathways",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPathways_ProgramConfigurationId",
                table: "PatientPathways",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Email",
                table: "Patients",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Name",
                table: "Patients",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStage_Config_Order",
                table: "ProcessStages",
                columns: new[] { "TenantId", "ProgramConfigurationId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStages_ProgramConfigurationId",
                table: "ProcessStages",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramConfig_Tenant_Program",
                table: "ProgramConfigurations",
                columns: new[] { "TenantId", "ProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramConfigurations_ProgramId",
                table: "ProgramConfigurations",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponse_Pathway",
                table: "QuestionnaireResponses",
                columns: new[] { "TenantId", "PatientPathwayId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponse_Status",
                table: "QuestionnaireResponses",
                columns: new[] { "TenantId", "QuestionnaireId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_PatientPathwayId",
                table: "QuestionnaireResponses",
                column: "PatientPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_QuestionnaireId",
                table: "QuestionnaireResponses",
                column: "QuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaire_Active_Recurrence",
                table: "Questionnaires",
                columns: new[] { "TenantId", "IsActive", "RecurrenceType" });

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaire_Program",
                table: "Questionnaires",
                columns: new[] { "TenantId", "ProgramConfigurationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_ProgramConfigurationId",
                table: "Questionnaires",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_TenantRole",
                table: "RolePermissions",
                columns: new[] { "TenantId", "RoleName" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ProgramConfigurationId",
                table: "RolePermissions",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinitions_ContentTemplateId",
                table: "TaskDefinitions",
                column: "ContentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinitions_ProgramConfigurationId",
                table: "TaskDefinitions",
                column: "ProgramConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_Assigned",
                table: "TaskItems",
                columns: new[] { "TenantId", "AssignedTo" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_DueDate",
                table: "TaskItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_Status",
                table: "TaskItems",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_Tenant_Patient",
                table: "TaskItems",
                columns: new[] { "TenantId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_EnrollmentId",
                table: "TaskItems",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_PatientId",
                table: "TaskItems",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProgramId",
                table: "TaskItems",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskDefinitionId",
                table: "TaskItems",
                column: "TaskDefinitionId");

            migrationBuilder.CreateIndex(
                name: "UX_TenantConfig_Tenant_Key",
                table: "TenantConfigurations",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Hostname",
                table: "Tenants",
                column: "Hostname");

            migrationBuilder.CreateIndex(
                name: "IX_TriggerExecution_Dedup",
                table: "TriggerExecutions",
                columns: new[] { "TenantId", "DeduplicationKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TriggerExecution_Pathway",
                table: "TriggerExecutions",
                columns: new[] { "TenantId", "PatientPathwayId" });

            migrationBuilder.CreateIndex(
                name: "IX_TriggerExecution_Pending",
                table: "TriggerExecutions",
                columns: new[] { "TenantId", "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TriggerExecution_Scheduled",
                table: "TriggerExecutions",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_TriggerExecutions_PatientPathwayId",
                table: "TriggerExecutions",
                column: "PatientPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvent_Pathway",
                table: "WorkflowEvents",
                columns: new[] { "TenantId", "PatientPathwayId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvent_Timeline",
                table: "WorkflowEvents",
                columns: new[] { "TenantId", "EventOccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvent_Type_Visibility",
                table: "WorkflowEvents",
                columns: new[] { "TenantId", "EventType", "VisibleToPatient" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEvents_PatientPathwayId",
                table: "WorkflowEvents",
                column: "PatientPathwayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BusinessRules");

            migrationBuilder.DropTable(
                name: "CommunicationMessages");

            migrationBuilder.DropTable(
                name: "FollowUpRules");

            migrationBuilder.DropTable(
                name: "FormDefinitions");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "QuestionnaireResponses");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "TenantConfigurations");

            migrationBuilder.DropTable(
                name: "TriggerExecutions");

            migrationBuilder.DropTable(
                name: "WorkflowEvents");

            migrationBuilder.DropTable(
                name: "Questionnaires");

            migrationBuilder.DropTable(
                name: "TaskDefinitions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "PatientPathways");

            migrationBuilder.DropTable(
                name: "ContentTemplates");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "ProcessStages");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "ProgramConfigurations");

            migrationBuilder.DropTable(
                name: "Programs");
        }
    }
}
