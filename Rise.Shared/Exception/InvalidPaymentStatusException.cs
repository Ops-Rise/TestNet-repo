namespace Rise.Shared.Exceptions;

public class InvalidPaymentStatusException(string paymentId, string status)
    : System.Exception($"Payment with ID '{paymentId}' has an invalid status: '{status}'.");
