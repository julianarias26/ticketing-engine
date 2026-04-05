using System.Diagnostics;
using System.Text.Json;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Events;

namespace TicketingEngine.Infrastructure.Jobs;

public sealed class OutboxPublisherJob
{
    private static readonly ActivitySource _activity = new("TicketingEngine");

    private readonly IOutboxRepository _outbox;
    private readonly IAppDbContext     _db;
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<OutboxPublisherJob> _logger;

    public OutboxPublisherJob(
        IOutboxRepository outbox, IAppDbContext db,
        IPublishEndpoint bus, ILogger<OutboxPublisherJob> logger)
    { _outbox = outbox; _db = db; _bus = bus; _logger = logger; }

    [AutomaticRetry(Attempts = 5, DelaysInSeconds = [10, 30, 60, 120, 300])]
    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        using var span = _activity.StartActivity("OutboxPublisher.ProcessBatch");
        var sw = Stopwatch.StartNew();

        var messages = await _outbox.GetPendingAsync(batchSize: 50, ct);
        span?.SetTag("batch.size", messages.Count);

        if (messages.Count == 0) return;

        int published = 0, failed = 0;

        foreach (var msg in messages)
        {
            try
            {
                var domainEvent = Deserialize(msg.EventType, msg.Payload);
                await _bus.Publish(domainEvent, domainEvent.GetType(), ct);
                msg.ProcessedAt = DateTime.UtcNow;
                published++;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                failed++;
                _logger.LogWarning(ex,
                    "Outbox message {Id} failed (retry {Count})", msg.Id, msg.RetryCount);
            }
        }

        await _db.SaveChangesAsync(ct);
        sw.Stop();

        span?.SetTag("published", published);
        span?.SetTag("failed",    failed);

        _logger.LogInformation(
            "Outbox batch: {Published} published, {Failed} failed in {Ms}ms",
            published, failed, sw.ElapsedMilliseconds);
    }

    private static object Deserialize(string eventType, string payload) =>
        eventType switch
        {
            nameof(SeatReservedEvent)     => JsonSerializer.Deserialize<SeatReservedEvent>(payload)!,
            nameof(SeatReleasedEvent)     => JsonSerializer.Deserialize<SeatReleasedEvent>(payload)!,
            nameof(PaymentProcessedEvent) => JsonSerializer.Deserialize<PaymentProcessedEvent>(payload)!,
            nameof(OrderExpiredEvent)     => JsonSerializer.Deserialize<OrderExpiredEvent>(payload)!,
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
}
