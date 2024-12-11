using Rise.Domain.Bookings;
using Rise.Fakers.BoatFakers;
using Rise.Fakers.BatteryFakers;
using Rise.Fakers.User;
using Rise.Fakers.Common;
using Rise.Domain.Prices;

namespace Rise.Fakers.BookingFakers;

public sealed class BookingFaker : EntityFaker<Booking>
{
    private readonly Dictionary<DateTime, int> bookings = new();

    public BookingFaker(BoatFaker boatFaker, BatteryFaker batteryFaker, UserFaker userFaker, Price price, string locale = "nl") : base(locale)
    {
        CustomInstantiator(f =>
        {
            var user = userFaker.Generate();
            var boat = f.Random.Bool() ? boatFaker.Generate() : null;
            var battery = f.Random.Bool() ? batteryFaker.Generate() : null;

            var timeSlots = new[]
{
                new TimeSpan(9, 0, 0),
                new TimeSpan(12, 0, 0),
                new TimeSpan(15, 0, 0)
            };

            DateTime rentalDateTime;

            // Generate a valid rental Date Time
            while (true)
            {
                var rentalDate = f.Date.Between(
                    DateTime.Today.AddDays(3),
                    DateTime.Today.AddDays(30)
                ).Date;

                var candidateDateTime = rentalDate + f.PickRandom(timeSlots);

                // Max 3 bookings for each time slot
                if (!bookings.TryGetValue(candidateDateTime, out var count) || count < 3)
                {
                    rentalDateTime = candidateDateTime;
                    bookings[candidateDateTime] = count + 1;
                    break;
                }
            }

            return new Booking(
                boat,
                battery,
                rentalDateTime,
                f.PickRandom<BookingStatus>(),
                user,
                price
            )
            {
                Remark = f.Random.Bool() ? f.Lorem.Sentence(5, 10) : null
            };
        });
    }
}
