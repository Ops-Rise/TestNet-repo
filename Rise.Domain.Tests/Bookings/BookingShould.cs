using Rise.Domain.Addresses;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.Users;
using Rise.Domain.Prices;
using Shouldly;

namespace Rise.Domain.Tests.Bookings;

public class BookingShould
{
    private readonly Boat _testBoat = new Boat("testBoat", BoatStatus.Available);
    private readonly DateTime _testBookingDate = DateTime.Now.AddDays(10);
    private readonly BookingStatus _testBookingStatus = BookingStatus.Active;

    private static readonly Address _testAddress = new Address(
        "testStreet",
        "1",
        "testCity",
        "9000"
    );

    private static readonly User _testUser = new User("auth0|testUserId","test@gmail.com")
    {
        Firstname = "TestFirstName",
        Lastname = "TestLastName",
        BirthDay = new DateTime(1990, 1, 1), // Example birthdate
        PhoneNumber = "0488776655",
        Address = _testAddress,
        IsRegistrationComplete = true,
    };

    private readonly Battery _testBattery = new Battery(
        "testBattery",
        BatteryStatus.Available,
        _testUser
    );

    private readonly Price _testPrice = new Price(100.00m);

    [Fact]
    public void BeCreated()
    {
        Booking testBooking = new Booking(
            _testBoat,
            _testBattery,
            _testBookingDate,
            _testBookingStatus,
            _testUser,
            _testPrice
        );

        testBooking.ShouldNotBeNull();
        testBooking.Boat.ShouldBe(_testBoat);
        testBooking.RentalDateTime.ShouldBe(_testBookingDate);
        testBooking.Status.ShouldBe(_testBookingStatus);
        testBooking.User.ShouldBe(_testUser);
    }

    [Fact]
    public void NotBeCreatedWithNullUser()
    {
        User testUser = null!;

        Action act = () =>
        {
            Booking testBooking = new Booking(
                _testBoat,
                _testBattery,
                _testBookingDate,
                _testBookingStatus,
                testUser,
                _testPrice
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'value')");
    }

    [Fact]
    public void BeCreatedWithNullBoatAndNullBattery()
    {
        Boat testBoat = null!;
        Battery testBattery = null!;

        Booking testBooking = new Booking(
            testBoat,
            testBattery,
            _testBookingDate,
            _testBookingStatus,
            _testUser,
            _testPrice
        );

        testBooking.ShouldNotBeNull();
        testBooking.Boat.ShouldBe(null);
        testBooking.Battery.ShouldBe(null);
        testBooking.RentalDateTime.ShouldBe(_testBookingDate);
        testBooking.Status.ShouldBe(_testBookingStatus);
        testBooking.User.ShouldBe(_testUser);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(31)]
    public void NotBeCreatedWithWrongBookingDate(int days)
    {
        DateTime testBookingDate = DateTime.Now.AddDays(days);

        Action act = () =>
        {
            Booking testBooking = new Booking(
                _testBoat,
                _testBattery,
                testBookingDate,
                _testBookingStatus,
                _testUser,
                _testPrice
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            $"RentalDateTime must be between {Booking.MinAdvanceDays} and {Booking.MaxAdvanceDays} days from now."
        );
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(29)]
    [InlineData(30)]
    public void BeCreatedWithRightBookingDate(int days)
    {
        DateTime testBookingDate = DateTime.Now.AddDays(days);

        Booking testBooking = new Booking(
            _testBoat,
            _testBattery,
            testBookingDate,
            _testBookingStatus,
            _testUser,
            _testPrice
        );

        testBooking.ShouldNotBeNull();
        testBooking.Boat.ShouldBe(_testBoat);
        testBooking.RentalDateTime.ShouldBe(testBookingDate);
        testBooking.Status.ShouldBe(_testBookingStatus);
        testBooking.User.ShouldBe(_testUser);
    }

    [Fact]
    public void NotBeCreatedWithNullBookingDate()
    {
        DateTime testBookingDate = default!;

        Action act = () =>
        {
            Booking testBooking = new Booking(
                _testBoat,
                _testBattery,
                testBookingDate,
                _testBookingStatus,
                _testUser,
                _testPrice
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            $"RentalDateTime must be between {Booking.MinAdvanceDays} and {Booking.MaxAdvanceDays} days from now."
        );
    }
}
