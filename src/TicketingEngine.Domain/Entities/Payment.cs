using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string ProviderTransactionId { get; private set; } = default!;
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid orderId, string provider,
        string providerTxId, decimal amount) => new()
        {
            OrderId               = orderId,
            Provider              = provider,
            ProviderTransactionId = providerTxId,
            Status                = PaymentStatus.Pending,
            Amount                = amount
        };

    public void MarkSucceeded() { Status = PaymentStatus.Succeeded; ProcessedAt = DateTime.UtcNow; }
    public void MarkFailed()    { Status = PaymentStatus.Failed;    ProcessedAt = DateTime.UtcNow; }
}
