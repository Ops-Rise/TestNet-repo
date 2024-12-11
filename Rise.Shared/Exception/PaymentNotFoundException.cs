namespace Rise.Shared.Exceptions;

public class PaymentNotFoundException(string paymentId) : System.Exception($"Payment with ID '{paymentId}' was not found.");
