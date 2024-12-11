using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Payments;
using Rise.Shared.Prices;
using Rise.Shared.Users;
using Rise.Domain.Bookings;
using Rise.Shared.Weather;

namespace Rise.Shared.Bookings;

public abstract class BookingDto
{


    public class Index
    {
        public required int Id { get; set; }
        public required DateTime RentalDateTime { get; set; }

        public BoatDto.BoatIndex? Boat { get; set; } 

        public BatteryDto.BatteryIndex? Battery { get; set; }
        public UserDto.Index? User { get; set; }
        public BookingStatus Status { get; set; }
        public PaymentResponseDto? Payment { get; set; } 
        public WeatherDto? Weather { get; set; }
    }

    public class Detail
    {
        public int Id { get; set; }
        public DateTime RentalDateTime { get; set; }

        public BoatDto.BoatIndex? Boat { get; set; } // Ensures BoatName is not null

        public BookingStatus Status { get; set; }

        public BatteryDto.BatteryDetail? Battery { get; set; }

        public UserDto.Index? User { get; set; }

        public string? Remark { get; set; }
        public PriceDto.Index? Price { get; set; }
        public PaymentResponseDto? Payment { get; set; } 
    }

    public class Mutate
    {
        public DateTime RentalDateTime { get; set; }

        public int? BoatId { get; set; }

        public int? BatteryId { get; set; }

        public int UserId { get; set; }
        public BookingStatus Status { get; set; }
        public int PriceId{ get; set; }  

        public string? Remark { get; set; }
    }

    public class Edit
    {
        public int Id { get; set; }
        public BoatDto.BoatIndex? Boat { get; set; }

        public DateTime RentalDateTime { get; set; }
        public BookingStatus Status { get; set; }
    }
}
