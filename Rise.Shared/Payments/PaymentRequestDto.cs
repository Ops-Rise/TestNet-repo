namespace Rise.Shared.Payments;

public class PaymentRequestDto
{

    public PaymentRequestDto(string description, decimal amount)
    {
        Description = description;
        Amount = new AmountDto(amount);
        
    }
    
    public PaymentRequestDto(string description, decimal amount, string redirectUrl)
    {
        Description = description;
        Amount = new AmountDto(amount);
        RedirectUrl = redirectUrl;
        
    }
    public AmountDto Amount { get; set; }
    public string Description { get; set; }
    public string RedirectUrl { get; set; }
}
