using Rise.Domain.Bookings;

namespace Rise.Domain.Prices;

public class Price : Entity
{
    private decimal amount = default!;

    public decimal Amount
    {
        get => amount;
        set
        {
            if (value < 0)
            {
                throw new ArgumentException("Amount cannot be negative.");
            }

            if (decimal.Round(value, 2) != value)
            {
                throw new ArgumentException("Amount must have at most two decimal places.");
            }

            amount = value;
        }
    }

    public ICollection<Booking> Bookings { get; } = new List<Booking>();

    private Price() { }

    public Price(decimal amount)
    {
        Amount = amount;
    }
}
