using Asp.Versioning;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using TicketingEngine.API.Extensions;
using TicketingEngine.API.Filters;
using TicketingEngine.API.Hubs;
using TicketingEngine.API.Middleware;
using TicketingEngine.Application.Behaviors;
using TicketingEngine.Infrastructure;
using TicketingEngine.Infrastructure.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<
        TicketingEngine.Application.Commands.ReserveSeat.ReserveSeatCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<
    TicketingEngine.Application.Commands.ReserveSeat.ReserveSeatCommandValidator>();

// ── Auth ─────────────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        var key = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured.");
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey        = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(key)),
            ValidateIssuer          = true,
            ValidIssuer             = builder.Configuration["Jwt:Issuer"],
            ValidateAudience        = true,
            ValidAudience           = builder.Configuration["Jwt:Audience"],
            ClockSkew               = TimeSpan.Zero
        };
        // Allow SignalR to read JWT from query string
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:ConnectionString"]!));

// ── API / Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion  = new ApiVersion(1);
    opts.AssumeDefaultVersionWhenUnspecified = true;
    opts.ReportApiVersions  = true;
}).AddApiExplorer(opts =>
{
    opts.GroupNameFormat           = "'v'VVV";
    opts.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "Ticketing Engine API", Version = "v1" });
    opts.AddSecurityDefinition("Bearer", new()
    {
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT token. Format: Bearer {token}",
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "bearer"
    });
    opts.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Id = "Bearer",
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme } },
            []
        }
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!);

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Run migrations and seed on startup
await app.MigrateAndSeedAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<WaitingRoomHub>("/hubs/waiting-room");
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()]
});

RecurringJob.AddOrUpdate<OutboxPublisherJob>(
    "outbox-publisher",
    job => job.ProcessPendingAsync(CancellationToken.None),
    "*/30 * * * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

app.Run();

public partial class Program { } // needed for WebApplicationFactory in tests
