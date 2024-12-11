using Rise.Fakers.User;
using Rise.Fakers.BatteryFakers;
using Rise.Fakers.BoatFakers;
using Rise.Fakers.BookingFakers;
using Rise.Fakers.ProfileImageFakers;
using Rise.Domain.Prices;
using Rise.Domain.ProfileImages;


// The Seeder class is responsible for seeding the database with initial data.
// This includes addresses, users, boats, batteries, and bookings.
// You can add more Objects by changing the amount in the Generate() method
// and adding the corresponding seed method.

namespace Rise.Persistence;

public class Seeder
{
    private readonly ApplicationDbContext dbContext;

    public Seeder(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public void Seed()
    {
        if (!dbContext.Addresses.Any())
            SeedAddresses();

        if (!dbContext.Users.Any())
            SeedUsers();

        if (!dbContext.Boats.Any())
            SeedBoats();

        if (!dbContext.Batteries.Any())
            SeedBatteries();

        if (!dbContext.ProfileImages.Any())
            SeedProfileImages();

        if (!dbContext.Prices.Any())
            SeedPrices();

        if (!dbContext.Bookings.Any())
            SeedBookings();
    }

    private void SeedBoats()
    {
        var boatFaker = new BoatFaker().AsTransient();
        var boats = boatFaker.Generate(6);

        dbContext.Boats.AddRange(boats);
        dbContext.SaveChanges();
    }

    private void SeedAddresses()
    {
        var addressFaker = new AddressFaker().AsTransient();
        var addresses = addressFaker.Generate(20);

        dbContext.Addresses.AddRange(addresses);
        dbContext.SaveChanges();
    }

    private void SeedUsers()
    {
        var addresses = dbContext.Addresses.ToList();
        if (!addresses.Any())
            throw new Exception("No addresses available for user seeding.");

        var userFaker = new UserFaker(new AddressFaker()).AsTransient();
        var users = userFaker.Generate(15);

        var f = new Bogus.Faker();
        var usedAddresses = new HashSet<int?>();

        // Assign existing addresses to users
        foreach (var user in users)
        {
            var availableAddresses = addresses.Where(a => !usedAddresses.Contains(a.Id)).ToList();

            var selectedAddress = availableAddresses[Random.Shared.Next(availableAddresses.Count)];
            user.Address = selectedAddress;
            user.AddressId = selectedAddress.Id;

            usedAddresses.Add(user.AddressId);

            if (user.AddressId != null)
            {
                user.IsRegistrationComplete = true;

                user.IsTrainingComplete = user.IsRegistrationComplete && f.Random.Bool();
            }
        }

        dbContext.Users.AddRange(users);
        dbContext.SaveChanges();

        //Uncomment if we need Auth0 users =>

        var auth0AddressFaker = new AddressFaker().AsTransient();
        var auth0Addresses = auth0AddressFaker.Generate(3);
        dbContext.Addresses.AddRange(auth0Addresses);
        dbContext.SaveChanges();

        // Add Auth0 Users
        var specificUsers = new List<Rise.Domain.Users.User>
        {
            new Rise.Domain.Users.User("auth0|123456", "John.Doe@gmail.com")
            {
                Firstname = "John",
                Lastname = "Doe",
                BirthDay = new DateTime(1985, 5, 10),
                PhoneNumber = "0491234567",
                Address = auth0Addresses[0],
                IsRegistrationComplete = true,
            },
            new Rise.Domain.Users.User("auth0|654321", "Jane.Smith@gmail.com")
            {
                Firstname = "Jane",
                Lastname = "Smith",
                BirthDay = new DateTime(1990, 7, 15),
                PhoneNumber = "0491234565",
                Address = auth0Addresses[1],
                IsRegistrationComplete = true,
            },
            new Rise.Domain.Users.User("auth0|67335cf5486ab548a84cfb9b", "testuser@mail.be")
            {
                Firstname = "Simon",
                Lastname = "Peeter",
                BirthDay = new DateTime(1988, 11, 24),
                PhoneNumber = "0498123456",
                Address = auth0Addresses[2],
                IsRegistrationComplete = true,
            }
        };

        // Add specific users to the list
        users.AddRange(specificUsers);
        dbContext.SaveChanges();
    }

    private void SeedProfileImages()
    {
        var userIds = dbContext.Users
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .ToList();

        if (userIds.Count == 0)
        {
            throw new Exception("No users found to seed profile images.");
        }

        var profileImages = new List<ProfileImage>();

        foreach (var userId in userIds)
        {
            byte[] imageBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
            string contentType = "image/jpeg";

            var profileImage = new ProfileImage(userId, imageBytes, contentType);
            profileImages.Add(profileImage);
        }

        dbContext.ProfileImages.AddRange(profileImages);
        dbContext.SaveChanges();
    }

    /*private void SeedProfileImages()
    {
        var userIds = dbContext.Users
            .Where(u => !dbContext.ProfileImages.Any(p => p.UserId == u.Id))
            .OrderBy(u => u.Id)
            .Take(3)
            .Select(u => u.Id)
            .ToList();

        if (userIds.Count == 0)
        {
            throw new Exception("No users found to seed profile images.");
        }

        var profileImageFaker = new ProfileImageFaker(userIds).AsTransient();
        var profileImages = profileImageFaker.Generate(userIds.Count);

        dbContext.ProfileImages.AddRange(profileImages);
        dbContext.SaveChanges();
    }*/

    private void SeedBatteries()
    {
        var users = dbContext.Users.ToList();
        if (!users.Any())
            throw new Exception("No users available for battery seeding.");

        var batteryFaker = new BatteryFaker(new UserFaker(new AddressFaker())).AsTransient();
        var batteries = batteryFaker.Generate(8);

        foreach (var battery in batteries)
        {
            battery.User = users[Random.Shared.Next(users.Count)];
        }

        dbContext.Batteries.AddRange(batteries);
        dbContext.SaveChanges();

    }

    private void SeedBookings()
    {
        var users = dbContext.Users.ToList();
        var boats = dbContext.Boats.ToList();
        var batteries = dbContext.Batteries.ToList();
        var price = dbContext.Prices.FirstOrDefault();

        if (!users.Any())
            throw new Exception("No users available for booking seeding.");
        if (!boats.Any())
            throw new Exception("No boats available for booking seeding.");
        if (!batteries.Any())
            throw new Exception("No batteries available for booking seeding.");

        var boatFaker = new BoatFaker();
        var batteryFaker = new BatteryFaker(new UserFaker(new AddressFaker()));
        var userFaker = new UserFaker(new AddressFaker());

        var bookingFaker = new BookingFaker(boatFaker, batteryFaker, userFaker, price).AsTransient();


        var bookings = bookingFaker.Generate(30);

        foreach (var booking in bookings)
        {
            if (booking.Boat != null)
            {
                booking.Boat = boats[Random.Shared.Next(boats.Count)];
                booking.BoatId = booking.Boat.Id;
            }

            if (booking.Battery != null)
            {
                booking.Battery = batteries[Random.Shared.Next(batteries.Count)];
                booking.BatteryId = booking.Battery.Id;
            }

            booking.User = users[Random.Shared.Next(users.Count)];
            booking.UserId = booking.User.Id;
        }

        dbContext.Bookings.AddRange(bookings);
        dbContext.SaveChanges();
    }

    private void SeedPrices()
    {
        var prices = new List<Price> { new Price(30m) };
        dbContext.Prices.AddRange(prices);
        dbContext.SaveChanges();
    }
}
