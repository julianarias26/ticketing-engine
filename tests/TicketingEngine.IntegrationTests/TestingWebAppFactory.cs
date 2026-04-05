using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;
using TicketingEngine.Infrastructure.Persistence;
using Xunit;

namespace TicketingEngine.IntegrationTests;

public sealed class TestingWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("ticketing_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithUsername("admin").WithPassword("admin").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace Postgres connection
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));

            // Override connection strings
            builder.UseSetting(
                "ConnectionStrings:Postgres", _postgres.GetConnectionString());
            builder.UseSetting(
                "Redis:ConnectionString", _redis.GetConnectionString());
            builder.UseSetting(
                "RabbitMQ:Host", _rabbit.Hostname);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
        await _rabbit.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await _rabbit.DisposeAsync();
    }
}
