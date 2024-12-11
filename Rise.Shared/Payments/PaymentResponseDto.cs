namespace Rise.Shared.Payments
{
    public class PaymentResponseDto
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public AmountDto Amount { get; set; }
        public string? WebhookUrl { get; set; } // Nu optioneel
        public string? CheckoutUrl { get; set; } // Nu optioneel
        public DateTime CreatedAt { get; set; }

        public PaymentResponseDto() { }

        public PaymentResponseDto(
            string id,
            string status,
            AmountDto amount,
            DateTime createdAt,
            string webhookUrl,
            string checkoutUrl
        )
        {
            Id = id;
            Status = status;
            Amount = amount;
            CreatedAt = createdAt;
            WebhookUrl = webhookUrl;
            CheckoutUrl = checkoutUrl;
        }

        public PaymentResponseDto(string id, string status, AmountDto amount, DateTime createdAt)
        {
            Id = id;
            Status = status;
            Amount = amount;
            CreatedAt = createdAt;
        }
    }
}
