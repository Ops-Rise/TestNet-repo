using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Rise.Persistence;
using Rise.Services.Mappers;
using Rise.Shared.Exceptions;
using Rise.Shared.Payments;

namespace Rise.Services.Payments;

public class MolliePaymentService(IPaymentClient paymentClient, ApplicationDbContext dbContext)
    : IPaymentService
{
    public async Task<bool> UpdatePaymentById(string paymentId)
    {
        var payment =
            await dbContext.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId)
            ?? throw new PaymentNotFoundException(paymentId);

        var molliePayment =
            await GetPaymentById(paymentId)
            ?? throw new InvalidPaymentStatusException(
                paymentId,
                "Payment details could not be retrieved."
            );

        // Update de status indien nodig
        if (payment.Status == molliePayment.Status)
            return true;

        payment.Status = molliePayment.Status;
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<string> CreatePayment(
        PaymentRequestDto paymentRequest,
        string userId,
        int bookingId
    )
    {
        var mollieRequest = new PaymentRequest
        {
            Amount = new Amount(paymentRequest.Amount.Currency, paymentRequest.Amount.Value),
            Description = paymentRequest.Description,
            RedirectUrl = paymentRequest.RedirectUrl,
        };

        var paymentResponse =
            await paymentClient.CreatePaymentAsync(mollieRequest)
            ?? throw new InvalidOperationException("De betaling kon niet worden aangemaakt.");

        dbContext.Payments.Add(
            PaymentMapper.MapToPayment(paymentRequest, userId, paymentResponse.Id, bookingId)
        );
        await dbContext.SaveChangesAsync();

        return paymentResponse.Links.Checkout?.Href ?? "Geen checkout link beschikbaar.";
    }

    public async Task<PaymentResponseDto?> GetPaymentById(string paymentId)
    {
        var paymentResponse = await paymentClient.GetPaymentAsync(paymentId);
        return new PaymentResponseDto
        {
            Id = paymentResponse.Id,
            Status = paymentResponse.Status,
            Amount = new AmountDto(
                paymentResponse.Amount.Currency,
                Decimal.Parse(paymentResponse.Amount.Value)
            ),
            WebhookUrl = paymentResponse.WebhookUrl,
            CheckoutUrl = paymentResponse.Links.Checkout?.Href,
            CreatedAt = paymentResponse.CreatedAt,
        };
    }

    public async Task<Payment> GetPaymentByBookingId(int bookingId)
    {
        var payment =
            await dbContext.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId)
            ?? throw new InvalidOperationException($"No payment found for BookingId: {bookingId}");

        var paymentResponse =
            paymentClient.GetPaymentAsync(payment.PaymentId)
            ?? throw new InvalidOperationException(
                $"No payment details found for PaymentId: {payment.PaymentId}"
            );

        payment.Status = paymentResponse.Result.Status;
        payment.Amount = decimal.Parse(paymentResponse.Result.Amount.Value);
        dbContext.Payments.Update(payment);
        await dbContext.SaveChangesAsync();

        return payment;
    }

    public async Task CancelPaymentById(int bookingId)
    {
        var payment =
            await dbContext.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId)
            ?? throw new InvalidOperationException($"No payment found for BookingId: {bookingId}");

        await paymentClient.CancelPaymentAsync(payment.PaymentId);
    }
}
