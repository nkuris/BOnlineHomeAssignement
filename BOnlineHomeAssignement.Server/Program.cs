using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Linq;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using BOnlineHomeAssignement.Server.Infrastructure.Repositories;
using BOnlineHomeAssignement.Server.HealthChecks;
using BOnlineHomeAssignement.Server.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS: allow requests from the local client dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowLocalDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Configure EF Core DbContext (reads connection string from env or config)
var conn = builder.Configuration.GetConnectionString("DefaultConnection") ?? builder.Configuration["ConnectionStrings:DefaultConnection"] ?? throw new InvalidOperationException("Missing DefaultConnection");

// Register both factory and regular DbContext for different lifetimes
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(conn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Also register AppDbContext for scoped resolution so background migration service can resolve it directly
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(conn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Register repository infrastructure (Unit of Work pattern)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register Patient Service
builder.Services.AddScoped<IPatientService, PatientService>();

// Register Lead Service
builder.Services.AddScoped<ILeadService, LeadService>();

// Register Appointment Service
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Register Lead Conversion Service
builder.Services.AddScoped<ILeadConversionService, LeadConversionService>();

// Register Workflow Services (Phase 3.5: Configurable programs, pathways, workflows)
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Register background migration service to apply migrations without blocking host startup
builder.Services.AddHostedService<BOnlineHomeAssignement.Server.Services.MigrationBackgroundService>();

// Health checks (including DB)
builder.Services.AddHealthChecks()
    .AddCheck<DbConnectionHealthCheck>("database");

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Migrations are applied by the MigrationBackgroundService at runtime so the host can start quickly.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health endpoint (returns JSON with checks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                description = kvp.Value.Description,
                duration = kvp.Value.Duration.TotalMilliseconds,
                exception = kvp.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
});

// Enable CORS before HTTPS redirection and authorization so preflight
// requests are handled correctly and not redirected.
app.UseCors("AllowLocalDev");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
