using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace TicketingEngine.API.Hubs;

[Authorize]
public sealed class WaitingRoomHub : Hub
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<WaitingRoomHub> _logger;

    public WaitingRoomHub(
        IConnectionMultiplexer redis,
        ILogger<WaitingRoomHub> logger)
    { _redis = redis; _logger = logger; }

    public async Task JoinEventRoom(Guid eventId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, $"event:{eventId}");

        var db = _redis.GetDatabase();
        var count = await db.StringIncrementAsync(
            $"waitroom:users:{eventId}");

        Context.Items["eventId"] = eventId;

        await Clients.Group($"event:{eventId}")
            .SendAsync("ActiveUsersUpdated", count);

        _logger.LogInformation(
            "Connection {Id} joined event room {EventId}",
            Context.ConnectionId, eventId);
    }

    public async Task LeaveEventRoom(Guid eventId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId, $"event:{eventId}");

        var db    = _redis.GetDatabase();
        var count = await db.StringDecrementAsync(
            $"waitroom:users:{eventId}");

        await Clients.Group($"event:{eventId}")
            .SendAsync("ActiveUsersUpdated", Math.Max(0, count));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("eventId", out var raw)
            && raw is Guid eventId)
            await LeaveEventRoom(eventId);

        await base.OnDisconnectedAsync(exception);
    }
}
