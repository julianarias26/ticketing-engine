using static System.Net.Mime.MediaTypeNames;
using System;
using TicketingEngine.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// API/Program.cs
builder.Services
    .AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(Application.AssemblyReference.Assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    })
    .AddValidatorsFromAssembly(Application.AssemblyReference.Assembly)
    .AddStackExchangeRedisCache(opts =>
        opts.Configuration = builder.Configuration["Redis:ConnectionString"])
    .AddDbContext<AppDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")))
    .AddHangfire(cfg => cfg.UsePostgreSqlStorage(
        builder.Configuration.GetConnectionString("Postgres")))
    .AddSignalR().AddStackExchangeRedis(
        builder.Configuration["Redis:ConnectionString"]!);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
