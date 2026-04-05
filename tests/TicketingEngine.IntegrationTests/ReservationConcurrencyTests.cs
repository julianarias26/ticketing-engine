//using System.Net;
//using System.Net.Http.Json;
//using FluentAssertions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using TicketingEngine.API.Controllers;
//using TicketingEngine.Application.Commands.ReserveSeat;
//using TicketingEngine.Domain.Entities;
//using TicketingEngine.Domain.ValueObjects;
//using TicketingEngine.Infrastructure.Persistence;
//using Xunit;

//namespace TicketingEngine.IntegrationTests;

//public sealed class ReservationConcurrencyTests
//    : IClassFixture<TestingWebAppFactory>
//{
//    private readonly TestingWebAppFactory _factory;
//    private readonly HttpClient _client;

//    public ReservationConcurrencyTests(TestingWebAppFactory factory)
//    {
//        _factory = factory;
//        _client  = factory.CreateClient();
//    }

//    /// <summary>
//    /// THE KEY TEST: 50 users compete for 1 seat simultaneously.
//    /// Exactly 1 must succeed, 49 must receive 409 Conflict.
//    /// Zero overselling tolerated.
//    /// </summary>
//    [Fact]
//    public async Task Reserve_WhenMultipleUsersConcurrent_OnlyOneSucceeds()
//    {
//        // Arrange
//        var seatId  = await CreateAvailableSeatAsync();
//        var eventId = await GetOrCreateEventIdAsync();

//        var concurrentUsers = 50;
//        var successCount    = 0;
//        var conflictCount   = 0;

//        // Act — 50 simultaneous requests
//        await Parallel.ForEachAsync(
//            Enumerable.Range(0, concurrentUsers),
//            new ParallelOptions { MaxDegreeOfParallelism = 50 },
//            async (_, ct) =>
//            {
//                var response = await _client.PostAsJsonAsync(
//                    "/api/v1/reservations",
//                    new ReserveSeatRequest(
//                        seatId, eventId, Guid.NewGuid().ToString()),
//                    ct);

//                if (response.IsSuccessStatusCode)
//                    Interlocked.Increment(ref successCount);
//                else if (response.StatusCode == HttpStatusCode.Conflict)
//                    Interlocked.Increment(ref conflictCount);
//            });

//        // Assert — exactly 1 winner, zero overselling
//        successCount.Should().Be(1,
//            because: "only one reservation can exist per seat");
//        conflictCount.Should().Be(concurrentUsers - 1,
//            because: "all other attempts must be rejected");

//        // Verify DB state
//        using var scope = _factory.Services.CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        var seat = await db.Seats.FindAsync(seatId);
//        seat!.Status.Should().Be(SeatStatus.Reserved);
//    }

//    [Fact]
//    public async Task Reserve_WithSameIdempotencyKey_IsIdempotent()
//    {
//        // Arrange
//        var seatId        = await CreateAvailableSeatAsync();
//        var eventId       = await GetOrCreateEventIdAsync();
//        var idempotentKey = Guid.NewGuid().ToString();

//        var request = new ReserveSeatRequest(seatId, eventId, idempotentKey);

//        // Act — send same request twice
//        var first  = await _client.PostAsJsonAsync("/api/v1/reservations", request);
//        var second = await _client.PostAsJsonAsync("/api/v1/reservations", request);

//        var r1 = await first.Content.ReadFromJsonAsync<ReserveSeatResult>();
//        var r2 = await second.Content.ReadFromJsonAsync<ReserveSeatResult>();

//        // Assert — same order ID returned
//        first.IsSuccessStatusCode.Should().BeTrue();
//        second.IsSuccessStatusCode.Should().BeTrue();
//        r1!.OrderId.Should().Be(r2!.OrderId);
//    }

//    private async Task<Guid> CreateAvailableSeatAsync()
//    {
//        using var scope   = _factory.Services.CreateScope();
//        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        var venueId       = Guid.NewGuid();
//        var sectionId     = Guid.NewGuid();
//        var seat          = Seat.Create(sectionId, "A", 1);

//        // Minimal seed without FKs for speed
//        await db.Database.ExecuteSqlRawAsync(
//            "INSERT INTO seats(id,section_id,row_label,seat_number,status,version) " +
//            "VALUES({0},{1},{2},{3},{4},{5})",
//            seat.Id, sectionId, "A", 1, "Available", 0);

//        return seat.Id;
//    }

//    private async Task<Guid> GetOrCreateEventIdAsync()
//    {
//        using var scope = _factory.Services.CreateScope();
//        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        var ev          = await db.Events.FirstOrDefaultAsync();
//        return ev?.Id ?? Guid.NewGuid();
//    }
//}
