using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Models.Payment;
using Rise.Persistence;
using Rise.Services.Mappers;
using Rise.Shared.Exceptions;
using Rise.Shared.Payments;
namespace Rise.Services.Payments;

public class DefaultPaymentService : IPaymentService
{
    private readonly ApplicationDbContext dbContext;

    // Voeg IHttpContextAccessor toe aan de constructor
    public DefaultPaymentService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> UpdatePaymentById(string paymentId)
    {
        var paymentExists = await dbContext.Payments
            .AnyAsync(p => p.PaymentId.ToString() == paymentId);

        if (!paymentExists)
            throw new PaymentNotFoundException(paymentId);

        return true;
    }


    public async Task<string> CreatePayment(PaymentRequestDto paymentRequest, string userId,int bookingId)
    {
        var payment = PaymentMapper.MapToPayment(paymentRequest, userId, Guid.NewGuid().ToString(),bookingId);
        payment.Status = PaymentStatus.Paid;
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        return  paymentRequest.RedirectUrl; // This also translates to the booking url but is more conform the mollie implementation
    }


    public async Task<PaymentResponseDto?> GetPaymentById(string paymentId)
    {
        var payment = await dbContext.Payments
                          .SingleOrDefaultAsync(p => p.PaymentId.ToString() == paymentId)
                      ?? throw new PaymentNotFoundException(paymentId);

        return new PaymentResponseDto
        {
            Id = payment.PaymentId,
            Status = payment.Status,
            Amount = new AmountDto
            ("EUR", payment.Amount),
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<Payment> GetPaymentByBookingId(int bookingId)
    {
        var payment = await dbContext.Payments
                          .SingleOrDefaultAsync(p => p.BookingId == bookingId)
                      ?? throw new InvalidOperationException($"No payment found for BookingId: {bookingId}");
        return payment;
    }

    public async Task CancelPaymentById(int bookingId)
    {
        var payment = await dbContext.Payments
                          .SingleOrDefaultAsync(p => p.BookingId == bookingId)
                      ?? throw new InvalidOperationException($"No payment found for BookingId: {bookingId}");


        payment.Status = PaymentStatus.Canceled;

        dbContext.Payments.Update(payment);
        await dbContext.SaveChangesAsync();
    }
}