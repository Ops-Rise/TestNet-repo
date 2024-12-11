using Rise.Persistence;
using Rise.Shared.Payments;

namespace Rise.Services.Mappers;

public abstract class PaymentMapper
{
    public static Payment MapToPayment(
        PaymentRequestDto paymentRequest,
        string userId,
        string paymentId,
        int bookingId
    )
    {
        return new Payment(
            paymentId,
            paymentRequest.Amount.Value,
            DateTime.UtcNow,
            userId,
            bookingId
        );
    }
}
