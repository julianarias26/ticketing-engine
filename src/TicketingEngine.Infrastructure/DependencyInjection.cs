using Hangfire;
using Hangfire.PostgreSql;
using Humanizer.Configuration;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Infrastructure.Cache;
using TicketingEngine.Infrastructure.Jobs;
using TicketingEngine.Infrastructure.Messaging.Consumers;
using TicketingEngine.Infrastructure.Observability;
using TicketingEngine.Infrastructure.Persistence;
using TicketingEngine.Infrastructure.Persistence.Repositories;

namespace TicketingEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // Validaciones de cadenas de conexión
        var pgConn = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Postgres'.");
        var redisConn = config["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Falta la cadena de conexión de Redis.");
        var rabbitHost = config["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("Falta la configuración 'RabbitMQ:Host'.");

        // EF Core + Postgres
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(pgConn, npg => npg.EnableRetryOnFailure(3)));
        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<AppDbContext>());

        // Repositorios
        services.AddScoped<ISeatRepository, SeatRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Cache
        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = redisConn);
        services.AddScoped<ICacheService, RedisCacheService>();

        // MassTransit + RabbitMQ
        // Registrar MassTransit solo si no estamos en modo lite
        if (!config.GetValue<bool>("LiteMode"))
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumer<SeatReservedConsumer>();
                x.AddConsumer<OrderExpiredConsumer>();
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(config["RabbitMQ:Host"], "/", h =>
                    {
                        h.Username(config["RabbitMQ:User"] ?? "guest");
                        h.Password(config["RabbitMQ:Pass"] ?? "guest");
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            });
        }

        // Hangfire
        if (!config.GetValue<bool>("LiteMode"))
        {
            services.AddHangfire(hf =>
                hf.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(pgConn);
                }));
            services.AddHangfireServer();
            services.AddScoped<OutboxPublisherJob>();
            services.AddScoped<ReservationExpiryJob>();
        }

        // Background jobs
        services.AddScoped<OutboxPublisherJob>();
        services.AddScoped<ReservationExpiryJob>();

        // Observabilidad
        services.AddObservability(config);

        return services;
    }
}

