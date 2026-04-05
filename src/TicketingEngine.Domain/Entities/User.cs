using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    private User() { }

    public static User Create(string email, string fullName, string passwordHash,
        UserRole role = UserRole.Customer) => new()
        {
            Email        = email.ToLowerInvariant(),
            FullName     = fullName,
            PasswordHash = passwordHash,
            Role         = role
        };
}
