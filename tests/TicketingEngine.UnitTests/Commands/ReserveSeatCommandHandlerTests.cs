using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketingEngine.Application.Commands.ReserveSeat;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.Exceptions;
using TicketingEngine.Domain.ValueObjects;
using TicketingEngine.UnitTests.Builders;
using Xunit;

namespace TicketingEngine.UnitTests.Commands;

public sealed class ReserveSeatCommandHandlerTests
{
    private readonly Mock<ISeatRepository>        _seats        = new();
    private readonly Mock<IOrderRepository>       _orders       = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IOutboxRepository>      _outbox       = new();
    private readonly Mock<IAppDbContext>           _db           = new();
    private readonly Mock<IDistributedCache>       _cache        = new();

    private ReserveSeatCommandHandler CreateSut() =>
        new(_seats.Object, _orders.Object, _reservations.Object,
            _outbox.Object, _db.Object, _cache.Object,
            NullLogger<ReserveSeatCommandHandler>.Instance);

    private ReserveSeatCommand BuildCommand(Guid? seatId = null) =>
        new(seatId ?? Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid().ToString());

    [Fact]
    public async Task Handle_WhenSeatAvailable_ShouldReturnResult()
    {
        // Arrange
        var section = Section.Create(Guid.NewGuid(), "Floor", 100, 150_000m);
        var seat    = new SeatBuilder().AsAvailable().Build();
        // Inject section via reflection (private setter)
        typeof(Seat).GetProperty("Section")!
            .SetValue(seat, section);

        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
              .ReturnsAsync((byte[]?)null);
        _seats.Setup(r => r.GetByIdWithLockAsync(seat.Id, default))
              .ReturnsAsync(seat);
        _db.Setup(d => d.SaveChangesAsync(default)).ReturnsAsync(1);

        var cmd = BuildCommand(seat.Id);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, default);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().NotBeEmpty();
        result.ReservationId.Should().NotBeEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.TotalAmount.Should().Be(section.BasePrice);

        _db.Verify(d => d.SaveChangesAsync(default), Times.Once);
        _outbox.Verify(o => o.Add(It.IsAny<OutboxMessage>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSeatAlreadyReserved_ShouldThrow()
    {
        // Arrange
        var seat = new SeatBuilder().AsReserved().Build();
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
              .ReturnsAsync((byte[]?)null);
        _seats.Setup(r => r.GetByIdWithLockAsync(seat.Id, default))
              .ReturnsAsync(seat);

        var sut = CreateSut();
        var cmd = BuildCommand(seat.Id);

        // Act
        var act = async () => await sut.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<SeatAlreadyReservedException>();
        _db.Verify(d => d.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSeatNotFound_ShouldThrow()
    {
        // Arrange
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
              .ReturnsAsync((byte[]?)null);
        _seats.Setup(r => r.GetByIdWithLockAsync(id, default))
              .ReturnsAsync((Seat?)null);

        var sut = CreateSut();
        var act = async () => await sut.Handle(BuildCommand(id), default);

        // Assert
        await act.Should().ThrowAsync<SeatNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithSameIdempotencyKey_ShouldReturnCachedResult()
    {
        // Arrange
        var cachedResult = new ReserveSeatResult(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(15), 150_000m);
        var cachedBytes  = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(cachedResult));

        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), default))
              .ReturnsAsync(cachedBytes);

        var sut = CreateSut();
        var cmd = BuildCommand();

        // Act
        var result = await sut.Handle(cmd, default);

        // Assert
        result.OrderId.Should().Be(cachedResult.OrderId);
        _db.Verify(d => d.SaveChangesAsync(default), Times.Never);
        _seats.Verify(r => r.GetByIdWithLockAsync(It.IsAny<Guid>(), default),
            Times.Never);
    }
}
