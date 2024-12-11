using System;
using System.Collections.Generic;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.Users;
using Shouldly;

namespace Rise.Domain.Tests.Prices;

public class PriceShould
{
    [Fact]
    public void BeCreatedWithValidAmount()
    {
        decimal validAmount = 99.99m;
        Price price = new Price(validAmount);

        price.ShouldNotBeNull();
        price.Amount.ShouldBe(validAmount);
        price.Bookings.ShouldBeEmpty();
    }

    [Fact]
    public void NotBeCreatedWithNegativeAmount()
    {
        decimal negativeAmount = -50.00m;

        Action act = () =>
        {
            Price price = new Price(negativeAmount);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Amount cannot be negative.");
    }

    [Fact]
    public void NotBeCreatedWithMoreThanTwoDecimalPlaces()
    {
        decimal invalidAmount = 99.999m;

        Action act = () =>
        {
            Price price = new Price(invalidAmount);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Amount must have at most two decimal places.");
    }

    [Fact]
    public void AllowValidAmountUpdate()
    {
        Price price = new Price(50.00m);
        decimal newValidAmount = 75.99m;

        price.Amount = newValidAmount;

        price.Amount.ShouldBe(newValidAmount);
    }

    [Fact]
    public void NotAllowNegativeAmountUpdate()
    {
        Price price = new Price(50.00m);

        Action act = () =>
        {
            price.Amount = -10.00m;
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Amount cannot be negative.");
    }

    [Fact]
    public void NotAllowAmountUpdateWithMoreThanTwoDecimalPlaces()
    {
        Price price = new Price(50.00m);

        Action act = () =>
        {
            price.Amount = 99.999m;
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Amount must have at most two decimal places.");
    }

    [Fact]
    public void InitializeWithEmptyBookingsCollection()
    {
        Price price = new Price(50.00m);

        price.Bookings.ShouldNotBeNull();
        price.Bookings.ShouldBeEmpty();
    }

    [Fact]
    public void AllowAddingBookingsToCollection()
    {
        Price price = new Price(50.00m);
        Boat boat = new Boat("testBoat", BoatStatus.Available);
        Battery battery = new Battery(
            "testBattery",
            BatteryStatus.Available,
            new User("auth0UserId", "test@example.com")
        );
        DateTime bookingDate = DateTime.Now.AddDays(5);
        BookingStatus status = BookingStatus.Active;
        User user = new User("auth0UserId", "test@example.com");

        Booking booking1 = new Booking(boat, battery, bookingDate, status, user, price);
        Booking booking2 = new Booking(boat, battery, bookingDate, status, user, price);

        price.Bookings.Add(booking1);
        price.Bookings.Add(booking2);

        price.Bookings.Count.ShouldBe(2);
        price.Bookings.ShouldContain(booking1);
        price.Bookings.ShouldContain(booking2);
    }
}
