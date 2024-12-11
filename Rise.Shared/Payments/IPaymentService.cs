using Rise.Persistence;

namespace Rise.Shared.Payments;

public interface IPaymentService
{
    /// <summary>
    /// UpdatePaymentById is a method only to be used by the webhook controller.
    /// It updates the payment status based on a return from the payment provider.
    /// </summary>
    /// <param name="paymentId">PaymentId linked to a booking.</param>
    /// <returns>Returns true if the update was successful, false otherwise.</returns>
    Task<bool> UpdatePaymentById(string paymentId);

    /// <summary>
    /// Creates a payment request that will be sent to the payment provider.
    /// </summary>
    /// <param name="paymentRequest">Payment request containing Amount, Description, and RedirectUrl.</param>
    /// <returns>This returns the checkoutUrl, needed to redirect user to page.</returns>
    Task<string> CreatePayment(PaymentRequestDto paymentRequest, string userId, int bookingId);

    /// <summary>
    /// Retrieves the payment by Id from the payment provider.
    /// </summary>
    /// <param name="paymentId">PaymentId linked to a booking.</param>
    /// <returns>Payment details or null if not found.</returns>
    Task<PaymentResponseDto?> GetPaymentById(string paymentId);

    /// <summary>
    /// Retrieves a payment associated with a specific booking.
    /// </summary>
    /// <param name="bookingId">The unique identifier of the booking associated with the payment.</param>
    /// <returns>Returns a Payment entity associated with the booking, or throws an exception if no payment is found.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no payment is found for the given booking ID.</exception>
    /// <remarks>
    /// This method fetches the payment details linked to a booking directly from the database.
    /// It does not interact with the payment provider.
    /// Use this method to get internal payment data rather than real-time status from the provider.
    /// </remarks>
    Task<Payment> GetPaymentByBookingId(int bookingId);

    /// <summary>
    /// Cancels a payment associated with the given booking.
    /// </summary>
    /// <param name="bookingId">The unique identifier of the booking associated with the payment to be canceled.</param>
    /// <returns>A task representing the asynchronous operation. The task completes when the payment cancellation request is processed.</returns>
    /// <remarks>
    /// This method attempts to cancel the payment linked to the provided booking ID. If no payment exists for the given ID, an exception will be thrown.
    /// Use this method cautiously, as cancellation might not be reversible depending on the payment provider's policies.
    /// </remarks>
    Task CancelPaymentById(int bookingId);
}
