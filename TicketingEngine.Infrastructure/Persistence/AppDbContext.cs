using Microsoft.EntityFrameworkCore;

namespace TicketingEngine.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<Seat> Seats => Set<Seat>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
