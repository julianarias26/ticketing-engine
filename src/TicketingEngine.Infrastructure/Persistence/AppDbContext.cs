using Microsoft.EntityFrameworkCore;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Infrastructure.Persistence.Configurations;

namespace TicketingEngine.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>         Users        => Set<User>();
    public DbSet<Venue>        Venues       => Set<Venue>();
    public DbSet<Event>        Events       => Set<Event>();
    public DbSet<Section>      Sections     => Set<Section>();
    public DbSet<Seat>         Seats        => Set<Seat>();
    public DbSet<Order>        Orders       => Set<Order>();
    public DbSet<OrderItem>    OrderItems   => Set<OrderItem>();
    public DbSet<Reservation>  Reservations => Set<Reservation>();
    public DbSet<Payment>      Payments     => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
