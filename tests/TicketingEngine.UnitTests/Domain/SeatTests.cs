using FluentAssertions;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.Exceptions;
using TicketingEngine.Domain.ValueObjects;
using TicketingEngine.UnitTests.Builders;
using Xunit;

namespace TicketingEngine.UnitTests.Domain;

public sealed class SeatTests
{
    [Fact]
    public void Reserve_WhenAvailable_ShouldChangeStatusAndIncrementVersion()
    {
        // Arrange
        var seat = new SeatBuilder().AsAvailable().Build();
        var initialVersion = seat.Version;

        // Act
        seat.Reserve();

        // Assert
        seat.Status.Should().Be(SeatStatus.Reserved);
        seat.Version.Should().Be(initialVersion + 1);
        seat.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Reserve_WhenAlreadyReserved_ShouldThrowDomainException()
    {
        // Arrange
        var seat = new SeatBuilder().AsReserved().Build();

        // Act
        var act = () => seat.Reserve();

        // Assert
        act.Should().Throw<SeatAlreadyReservedException>()
            .WithMessage($"*{seat.Id}*");
    }

    [Fact]
    public void Release_WhenReserved_ShouldRestoreAvailability()
    {
        // Arrange
        var seat = new SeatBuilder().AsReserved().Build();

        // Act
        seat.Release();

        // Assert
        seat.Status.Should().Be(SeatStatus.Available);
        seat.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Release_WhenAlreadyAvailable_ShouldBeIdempotent()
    {
        // Arrange
        var seat    = new SeatBuilder().AsAvailable().Build();
        var version = seat.Version;

        // Act
        seat.Release(); // no-op

        // Assert
        seat.Version.Should().Be(version);
        seat.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkSold_WhenReserved_ShouldTransitionCorrectly()
    {
        // Arrange
        var seat = new SeatBuilder().AsReserved().Build();

        // Act
        seat.MarkSold();

        // Assert
        seat.Status.Should().Be(SeatStatus.Sold);
    }

    [Fact]
    public void MarkSold_WhenAvailable_ShouldThrow()
    {
        var seat = new SeatBuilder().AsAvailable().Build();
        var act  = () => seat.MarkSold();
        act.Should().Throw<InvalidSeatStatusTransitionException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyRowLabel_ShouldThrow(string label)
    {
        var act = () => Seat.Create(Guid.NewGuid(), label, 1);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidSeatNumber_ShouldThrow(int number)
    {
        var act = () => Seat.Create(Guid.NewGuid(), "A", number);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
