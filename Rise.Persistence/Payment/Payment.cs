using Rise.Domain.Common;

namespace Rise.Persistence;

public class Payment : Entity
{
    public Payment(
        string paymentId,
        decimal amount,
        DateTime timestamp,
        string userId,
        int bookingId
    )
    {
        PaymentId = paymentId;
        Amount = amount;
        Timestamp = timestamp;
        Status = "pending";
        UserId = userId;
        BookingId = bookingId;
    }

    public Payment(
        int id,
        string paymentId,
        decimal amount,
        DateTime timestamp,
        string status,
        string userId
    )
        : base(id)
    {
        PaymentId = paymentId;
        Amount = amount;
        Timestamp = timestamp;
        Status = status;
        UserId = userId;
    }

    public string PaymentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }
    public string UserId { get; set; }
    public int BookingId { get; set; }
}
