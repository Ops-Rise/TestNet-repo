using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rise.Domain.Batteries;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.Batteries.Services;
using Rise.Shared.Addresses;
using Rise.Shared.Batteries;
using Rise.Shared.Prices;
using Rise.Shared.Users;
using Shouldly;
using Xunit;

public class BatteryServiceTests
{
    private readonly Mock<DbSet<User>> _userDbSetMock;
    private string auth0UserId = "auth0|123";
    private string email = "test@example.com";

    public BatteryServiceTests()
    {
        _userDbSetMock = new Mock<DbSet<User>>();
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Gebruik een unieke database per test
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateBattery_ValidBattery_ShouldCreateBattery()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        BatteryDto.Create newBattery = new BatteryDto.Create
        {
            Name = "Battery 99",
            UserId = user.Id,
        };

        // Act
        var result = await batteryService.CreateBatteryAsync(newBattery);

        // Assert
        Battery battery = await dbContext.Batteries.FindAsync(result);
        result.ShouldBeGreaterThan(0);
        battery.Name.ShouldBe(newBattery.Name);
        battery.User.Id.ShouldBe(newBattery.UserId);
    }

    [Fact]
    public async Task CreateBattery_InValidBatteryNoExistingUser_ThrowsException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        BatteryDto.Create invalidBattery = new BatteryDto.Create
        {
            Name = "Battery 99",
            UserId = 1,
        };

        //Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await batteryService.CreateBatteryAsync(invalidBattery)
        );

        //Assert
        Assert.Equal("User does not exists.", exception.Message);
    }

    [Fact]
    public async Task CreateBattery_InValidBatteryDuplicateName_ThrowsException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery = new("Battery 99", BatteryStatus.Available, user);
        BatteryDto.Create newBattery = new BatteryDto.Create
        {
            Name = "Battery 99",
            UserId = user.Id,
        };
        dbContext.Batteries.Add(battery);
        await dbContext.SaveChangesAsync();

        //Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await batteryService.CreateBatteryAsync(newBattery)
        );

        //Assert
        Assert.Equal(
            $"A battery with the name {newBattery.Name} already exists.",
            exception.Message
        );
    }

    [Fact]
    public async Task GetAllBatteries_ReturnsAllBatteries()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery1 = new("Battery 99", BatteryStatus.Available, user);
        Battery battery2 = new("Battery 100", BatteryStatus.Reserve, user);
        dbContext.Batteries.AddRangeAsync(battery1, battery2);
        await dbContext.SaveChangesAsync();

        //Act
        var batteries = await batteryService.GetAllBatteriesAsync();

        //Assert
        batteries.ShouldNotBeNull();
        batteries.Count().ShouldBe(2);
        batteries.ShouldContain(x => x.Name == battery1.Name);
        batteries.ShouldContain(x => x.Name == battery2.Name);
    }

    [Fact]
    public async Task GetAllBatteries_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);

        //Act
        var batteries = await batteryService.GetAllBatteriesAsync();

        //Assert
        batteries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllBatteriesWithDetails_ReturnsAllBatteriesWithDetails()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Price price = new Price(30m);
        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync();
        Battery battery1 = new("Battery 99", BatteryStatus.Available, user);
        Battery battery2 = new("Battery 100", BatteryStatus.Reserve, user);
        dbContext.Batteries.AddRangeAsync(battery1, battery2);
        await dbContext.SaveChangesAsync();
        Booking booking1 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(5),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking2 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(18),
            BookingStatus.Canceled,
            user,
            price
        );
        Booking booking3 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(6),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking4 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(5),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking5 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(15),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking6 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(7),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking7 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(7),
            BookingStatus.Active,
            user,
            price
        );
        dbContext.Bookings.AddRangeAsync(
            booking1,
            booking2,
            booking3,
            booking4,
            booking5,
            booking6,
            booking7
        );
        await dbContext.SaveChangesAsync();

        //Act
        var batteries = await batteryService.GetAllBatteriesWithDetailsAsync();

        //Assert
        batteries.ShouldNotBeNull();
        batteries.Count().ShouldBe(2);
        batteries.ShouldContain(x => x.Name == battery1.Name);
        batteries.ShouldContain(x => x.Name == battery2.Name);
        BatteryDto.BatteryDetail batteryDto1 = batteries.FirstOrDefault(x =>
            x.Name == battery1.Name
        );
        batteryDto1.UseCycles.ShouldBe(2);
        batteryDto1.DateLastUsed.ShouldBe(booking3.RentalDateTime);
        BatteryDto.BatteryDetail batteryDto2 = batteries.FirstOrDefault(x =>
            x.Name == battery2.Name
        );
        batteryDto2.UseCycles.ShouldBe(4);
        batteryDto2.DateLastUsed.ShouldBe(booking5.RentalDateTime);
    }

    [Fact]
    public async Task GetAllBatteriesWithDetails_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);

        //Act
        var batteries = await batteryService.GetAllBatteriesWithDetailsAsync();

        //Assert
        batteries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetBatteriesByStatusAsync_ReturnCorrectBatteries()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery1 = new("Battery 99", BatteryStatus.Available, user);
        Battery battery2 = new("Battery 100", BatteryStatus.Reserve, user);
        Battery battery3 = new("Battery 101", BatteryStatus.Available, user);
        Battery battery4 = new("Battery 102", BatteryStatus.OutOfService, user);
        Battery battery5 = new("Battery 103", BatteryStatus.InRepair, user);
        Battery battery6 = new("Battery 104", BatteryStatus.InRepair, user);
        dbContext.Batteries.AddRangeAsync(
            battery1,
            battery2,
            battery3,
            battery4,
            battery5,
            battery6
        );
        await dbContext.SaveChangesAsync();

        //Act
        var availableBatteries = await batteryService.GetBatteriesByStatusAsync(
            BatteryStatus.Available
        );
        var inRepairBatteries = await batteryService.GetBatteriesByStatusAsync(
            BatteryStatus.InRepair
        );
        var outOfServiceBatteries = await batteryService.GetBatteriesByStatusAsync(
            BatteryStatus.OutOfService
        );
        var reserveBatteries = await batteryService.GetBatteriesByStatusAsync(
            BatteryStatus.Reserve
        );

        //Assert
        availableBatteries.ShouldNotBeNull();
        availableBatteries.Count().ShouldBe(2);
        availableBatteries.ShouldContain(x => x.Name == battery1.Name);
        availableBatteries.ShouldContain(x => x.Name == battery3.Name);

        inRepairBatteries.ShouldNotBeNull();
        inRepairBatteries.Count().ShouldBe(2);
        inRepairBatteries.ShouldContain(x => x.Name == battery5.Name);
        inRepairBatteries.ShouldContain(x => x.Name == battery6.Name);

        outOfServiceBatteries.ShouldNotBeNull();
        outOfServiceBatteries.Count().ShouldBe(1);
        outOfServiceBatteries.ShouldContain(x => x.Name == battery4.Name);

        reserveBatteries.ShouldNotBeNull();
        reserveBatteries.Count().ShouldBe(1);
        reserveBatteries.ShouldContain(x => x.Name == battery2.Name);
    }

    [Fact]
    public async Task GetBatteriesByStatusAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);

        //Act
        var batteries = await batteryService.GetBatteriesByStatusAsync(BatteryStatus.Available);

        //Assert
        batteries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetBatteryById_ValidId_ReturnBattery()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var battery1 = new Battery("Battery 99", BatteryStatus.Available, user);
        var battery2 = new Battery("Battery 100", BatteryStatus.Reserve, user);
        var battery3 = new Battery("Battery 101", BatteryStatus.Available, user);
        dbContext.Batteries.AddRangeAsync(battery1, battery2, battery3);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await batteryService.GetBatteryByIdAsync(battery2.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(battery2.Id);
        result.Name.ShouldBe(battery2.Name);
    }

    [Fact]
    public async Task GetBatteryById_InvalidId_ThrowsException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var battery1 = new Battery("Battery 99", BatteryStatus.Available, user);
        var battery2 = new Battery("Battery 100", BatteryStatus.Reserve, user);
        var battery3 = new Battery("Battery 101", BatteryStatus.Available, user);
        dbContext.Batteries.AddRangeAsync(battery1, battery2, battery3);
        await dbContext.SaveChangesAsync();
        int invalidId = battery1.Id + battery2.Id + battery3.Id;

        //Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await batteryService.GetBatteryByIdAsync(invalidId)
        );

        //Assert
        Assert.Equal($"Battery with id {invalidId} not found.", exception.Message);
    }

    [Fact]
    public async Task GetBatteryWithDetailsById_ValidId_ReturnBattery()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Price price = new Price(30m);
        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync();
        Battery battery1 = new("Battery 99", BatteryStatus.Available, user);
        Battery battery2 = new("Battery 100", BatteryStatus.Reserve, user);
        dbContext.Batteries.AddRangeAsync(battery1, battery2);
        await dbContext.SaveChangesAsync();
        Booking booking1 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(5),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking2 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(18),
            BookingStatus.Canceled,
            user,
            price
        );
        Booking booking3 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(6),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking4 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(5),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking5 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(15),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking6 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(7),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking7 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(7),
            BookingStatus.Active,
            user,
            price
        );
        dbContext.Bookings.AddRangeAsync(
            booking1,
            booking2,
            booking3,
            booking4,
            booking5,
            booking6,
            booking7
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result1 = await batteryService.GetBatteryWithDetailsByIdAsync(battery1.Id);
        var result2 = await batteryService.GetBatteryWithDetailsByIdAsync(battery2.Id);

        // Assert
        result1.ShouldNotBeNull();
        result1.Id.ShouldBe(battery1.Id);
        result1.Name.ShouldBe(battery1.Name);
        result1.UseCycles.ShouldBe(2);
        result1.DateLastUsed.ShouldBe(booking3.RentalDateTime);

        result2.ShouldNotBeNull();
        result2.Id.ShouldBe(battery2.Id);
        result2.Name.ShouldBe(battery2.Name);
        result2.UseCycles.ShouldBe(4);
        result2.DateLastUsed.ShouldBe(booking5.RentalDateTime);
    }

    [Fact]
    public async Task GetBatteryWithDetailsById_InvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Price price = new Price(30m);
        dbContext.Prices.Add(price);
        await dbContext.SaveChangesAsync();
        Battery battery1 = new("Battery 99", BatteryStatus.Available, user);
        Battery battery2 = new("Battery 100", BatteryStatus.Reserve, user);
        dbContext.Batteries.AddRangeAsync(battery1, battery2);
        await dbContext.SaveChangesAsync();
        Booking booking1 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(5),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking2 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(18),
            BookingStatus.Canceled,
            user,
            price
        );
        Booking booking3 = new Booking(
            null,
            battery1,
            DateTime.Now.AddDays(6),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking4 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(5),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking5 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(15),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking6 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(7),
            BookingStatus.Active,
            user,
            price
        );
        Booking booking7 = new Booking(
            null,
            battery2,
            DateTime.Now.AddDays(7),
            BookingStatus.Active,
            user,
            price
        );
        dbContext.Bookings.AddRangeAsync(
            booking1,
            booking2,
            booking3,
            booking4,
            booking5,
            booking6,
            booking7
        );
        await dbContext.SaveChangesAsync();

        int invalidId = battery1.Id + battery2.Id;

        //Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await batteryService.GetBatteryWithDetailsByIdAsync(invalidId)
        );

        //Assert
        Assert.Equal($"Battery with id {invalidId} not found.", exception.Message);
    }

    [Fact]
    public async Task UpdateBattery_ValidModel_ReturnsTrue()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery = new("Battery 99", BatteryStatus.Available, user);
        dbContext.Batteries.Add(battery);
        await dbContext.SaveChangesAsync();
        BatteryDto.Mutate updateModel = new BatteryDto.Mutate
        {
            Name = "Battery 100",
            Status = BatteryStatus.Reserve,
            UserId = user.Id,
        };

        // Act
        var result = await batteryService.UpdateBatteryAsync(battery.Id, updateModel);

        // Assert
        result.ShouldBeTrue();
        var updatedBattery = await dbContext.Batteries.FindAsync(battery.Id);
        updatedBattery.Name.ShouldBe(updateModel.Name);
        updatedBattery.Status.ShouldBe(BatteryStatus.Reserve);
    }

    [Fact]
    public async Task UpdateBattery_InValidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery = new("Battery 99", BatteryStatus.Available, user);
        dbContext.Batteries.Add(battery);
        await dbContext.SaveChangesAsync();
        BatteryDto.Mutate updateModel = new BatteryDto.Mutate
        {
            Name = "Battery 100",
            Status = BatteryStatus.Reserve,
            UserId = user.Id,
        };
        int invalidId = battery.Id + 1;

        //Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await batteryService.UpdateBatteryAsync(invalidId, updateModel)
        );

        //Assert
        Assert.Equal($"Battery with id {invalidId} not found.", exception.Message);
    }

    [Fact]
    public async Task DeleteBattery_ValidId_ReturnsTrue()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery = new("Battery 99", BatteryStatus.Available, user);
        dbContext.Batteries.Add(battery);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await batteryService.DeleteBatteryAsync(battery.Id);

        // Assert
        result.ShouldBeTrue();
        var deletedBattery = await dbContext.Batteries.FindAsync(battery.Id);
        deletedBattery.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteBattery_InValidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var batteryService = new BatteryService(dbContext);
        var user = new User(auth0UserId, email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Battery battery = new("Battery 99", BatteryStatus.Available, user);
        dbContext.Batteries.Add(battery);
        await dbContext.SaveChangesAsync();
        int invalidId = battery.Id + 1;

        //Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await batteryService.DeleteBatteryAsync(invalidId)
        );

        //Assert
        Assert.Equal($"Battery with id {invalidId} not found.", exception.Message);
    }
}
