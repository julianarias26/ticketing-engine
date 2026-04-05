using System.Diagnostics.Metrics;

namespace TicketingEngine.Infrastructure.Observability;

public sealed class TicketingMetrics
{
    private readonly Counter<long> _reservationsAttempted;
    private readonly Counter<long> _reservationsSucceeded;
    private readonly Counter<long> _reservationsFailed;
    private readonly Counter<long> _oversellPrevented;
    private readonly Counter<long> _ordersExpired;
    private readonly Histogram<double> _reservationDuration;
    private readonly Histogram<double> _outboxBatchDuration;

    public TicketingMetrics()
    {
        var m = TelemetrySetup.Meter;
        _reservationsAttempted = m.CreateCounter<long>(
            "ticketing.reservations.attempted", "reservations");
        _reservationsSucceeded = m.CreateCounter<long>(
            "ticketing.reservations.succeeded", "reservations");
        _reservationsFailed = m.CreateCounter<long>(
            "ticketing.reservations.failed", "reservations");
        _oversellPrevented = m.CreateCounter<long>(
            "ticketing.oversell.prevented", "attempts");
        _ordersExpired = m.CreateCounter<long>(
            "ticketing.orders.expired", "orders");
        _reservationDuration = m.CreateHistogram<double>(
            "ticketing.reservation.duration", "ms");
        _outboxBatchDuration = m.CreateHistogram<double>(
            "ticketing.outbox.batch_duration", "ms");
    }

    public void RecordAttempt(string eventId) =>
        _reservationsAttempted.Add(1, new[] { KeyValuePair.Create("event.id", (object?)eventId) });

    public void RecordSuccess(string eventId, double ms)
    {
        var tags = new[] { KeyValuePair.Create("event.id", (object?)eventId) };
        _reservationsSucceeded.Add(1, tags);
        _reservationDuration.Record(ms, tags);
    }

    public void RecordFailure(string eventId, string reason)
    {
        var tags = new[] {
            KeyValuePair.Create("event.id", (object?)eventId),
            KeyValuePair.Create("reason", (object?)reason)
        };
        _reservationsFailed.Add(1, tags);
        if (reason == "seat_already_reserved")
            _oversellPrevented.Add(1, new[] { KeyValuePair.Create("event.id", (object?)eventId) });
    }

    public void RecordOutboxBatch(double ms) =>
        _outboxBatchDuration.Record(ms);

    public void RecordOrderExpired() => _ordersExpired.Add(1);
}
