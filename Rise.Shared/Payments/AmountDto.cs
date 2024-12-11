namespace Rise.Shared.Payments
{
    public class AmountDto
    {
        public AmountDto() 
        {
            Currency = "EUR"; 
        }

        public AmountDto(string currency, decimal value)
        {
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            Value = value;
        }

        public AmountDto(decimal value)
        {
            Currency = "EUR";
            Value = value;
        }

        public string Currency { get; set; }
        public decimal Value { get; set; }
    }
}