using System.ComponentModel.DataAnnotations;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Prices;
using Rise.Domain.Users;

namespace Rise.Domain.Bookings;

public class Booking : Entity
{
    private Boat? boat;
    private Battery? battery;
    private Price price;

    private DateTime rentalDateTime = default!;
    private BookingStatus status = default!;
    private string? remark;
    public static readonly int MinAdvanceDays = 3;
    public static readonly int MaxAdvanceDays = 30;

    public Boat? Boat
    {
        get => boat;
        set => boat = value;
    }

    public int? BatteryId { get; set; }

    public int? BoatId { get; set; }

    public int UserId { get; set; }

    private User user = default!;

    public User User
    {
        get => user;
        set => user = Guard.Against.Null(value, nameof(value));
    }
    public Battery? Battery
    {
        get => battery;
        set => battery = value; // Uses nameof to pass the correct property name
    }

    public DateTime RentalDateTime
    {
        get => rentalDateTime;
        set
        {
            //enkel bij aanmaken boeking controle, bij update geen controle (kan dan wel <3dagen zijn)
            if (rentalDateTime == default)
            {
                var today = DateTime.Today;
                var minDate = today.AddDays(MinAdvanceDays); //0:00:00
                var maxDate = today
                    .AddDays(MaxAdvanceDays)
                    .AddHours(23)
                    .AddMinutes(59)
                    .AddSeconds(59); //23:59:59

                if (value < minDate || value > maxDate)
                {
                    throw new ArgumentException(
                        $"RentalDateTime must be between {MinAdvanceDays} and {MaxAdvanceDays} days from now."
                    );
                }
            }

            rentalDateTime = value;
        }
    }

    public BookingStatus Status
    {
        get => status;
        set => status = value;
    }

    [MaxLength(200, ErrorMessage = "Remark cannot exceed 200 characters.")]
    public string? Remark
    {
        get => remark;
        set => remark = value;
    }

    public Price Price
    {
        get => price;
        set => price = value;
    }

    public int PriceId { get; set; }

    private Booking() { }

    public Booking(
        Boat? boat,
        Battery? battery,
        DateTime rentalDateTime,
        BookingStatus status,
        User user,
        Price price
    )
    {
        Boat = boat;
        Battery = battery;
        RentalDateTime = rentalDateTime;
        Status = status;
        User = user;
        Price = price;
    }

    // Constructor with remark
    public Booking(
        Boat? boat,
        Battery? battery,
        DateTime rentalDateTime,
        BookingStatus status,
        User user,
        Price price,
        string remark
    ) : this(boat, battery, rentalDateTime, status, user, price)
    {
        Remark = remark;
    }
}
