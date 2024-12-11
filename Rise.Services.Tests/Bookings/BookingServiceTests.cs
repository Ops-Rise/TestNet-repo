using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.Bookings;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Emails;
using Rise.Shared.Emails.Models;
using Rise.Shared.Emails.Models;
using Rise.Shared.Prices;
using Rise.Shared.Weather;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Services.Tests
{
    public class BookingServiceTests : IDisposable
    {
        private readonly Mock<IBoatService> _mockBoatService;
        private readonly Mock<IEmailTemplateService> _mockTemplateService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly ApplicationDbContext _dbContext;
        private readonly BookingService _bookingService;
        private readonly ITestOutputHelper _output;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IEmailTemplateService> _emailTemplateService;

        private readonly Mock<IWeatherService> _weatherService;

        private string auth0UserId = "auth0|123";
        private string email = "test@example.com";

        public BookingServiceTests(ITestOutputHelper output)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use unique DB name per test
                .Options;

            _output = output;
            _dbContext = new ApplicationDbContext(options);
            _mockBoatService = new Mock<IBoatService>();
            _emailServiceMock = new Mock<IEmailService>();
            _emailTemplateService = new Mock<IEmailTemplateService>();
            _weatherService = new Mock<IWeatherService>();

            _bookingService = new BookingService(
                _dbContext,
                _mockBoatService.Object,
                _emailTemplateService.Object,
                _emailServiceMock.Object,
                _weatherService.Object
            );
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task CreateBookingAsync_ValidModelWithBoat_ReturnsBookingId()
        {
            Boat boat = new Boat("boat1", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(5),
                BoatId = boat.Id,
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            //Act
            var response = await _bookingService.CreateBookingAsync(model);
            var bookingId = response.bookingId;
            //Assert
            Assert.True(bookingId > 0);
            var createdBooking = await _dbContext.Bookings.FindAsync(bookingId);
            Assert.NotNull(createdBooking);
            Assert.Equal(model.RentalDateTime, createdBooking.RentalDateTime);
            Assert.Equal(boat.Id, createdBooking.Boat.Id);
            Assert.Null(createdBooking.Battery);
            Assert.Equal(BookingStatus.Active, createdBooking.Status);
            Assert.Equal(price.Id, createdBooking.Price.Id);
        }

        [Fact]
        public async Task CreateBookingAsync_ValidModelWithBattery_ReturnsBookingId()
        {
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            Battery battery = new("Battery 100", BatteryStatus.Available, user);
            _dbContext.Batteries.AddAsync(battery);
            await _dbContext.SaveChangesAsync();

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(3),
                BatteryId = battery.Id,
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            //Act
            var response = await _bookingService.CreateBookingAsync(model);
            var bookingId = response.bookingId;

            //Assert
            Assert.True(bookingId > 0);
            var createdBooking = await _dbContext.Bookings.FindAsync(bookingId);
            Assert.NotNull(createdBooking);
            Assert.Equal(model.RentalDateTime, createdBooking.RentalDateTime);
            Assert.Equal(battery.Id, createdBooking.Battery.Id);
            Assert.Null(createdBooking.Boat);
            Assert.Equal(BookingStatus.Active, createdBooking.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_ValidModelWithoutBatteryAndBoat_ReturnsBookingId()
        {
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(3),
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            //Act
            var response = await _bookingService.CreateBookingAsync(model);
            var bookingId = response.bookingId;

            //Assert
            Assert.True(bookingId > 0);
            var createdBooking = await _dbContext.Bookings.FindAsync(bookingId);
            Assert.NotNull(createdBooking);
            Assert.Equal(model.RentalDateTime, createdBooking.RentalDateTime);
            Assert.Null(createdBooking.Battery);
            Assert.Null(createdBooking.Boat);
            Assert.Equal(BookingStatus.Active, createdBooking.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_TimeSlotIsFullyBooked_ThrowsInvalidOperationException()
        {
            // Arrange
            Boat boat = new Boat("boat1", BoatStatus.Available);
            await _dbContext.Boats.AddRangeAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Now.AddDays(5);
            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var existingBooking = new Booking(
                boat,
                null,
                rentalDateTime,
                BookingStatus.Active,
                user2,
                price
            );
            await _dbContext.Bookings.AddAsync(existingBooking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                RentalDateTime = rentalDateTime,
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CreateBookingAsync(model)
            );
            Assert.Equal("The specified rental date and time are fully booked.", exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WithBoatThatsAlreadyBookedOnSameTimeSlot_ThrowsInvalidOperationException()
        {
            // Arrange
            Boat boat = new Boat("boat1", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Now.AddDays(5);

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(2);

            var existingBooking = new Booking(
                boat,
                null,
                rentalDateTime,
                BookingStatus.Active,
                user2,
                price
            );
            await _dbContext.Bookings.AddAsync(existingBooking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BoatId = boat.Id,
                RentalDateTime = rentalDateTime,
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CreateBookingAsync(model)
            );
            Assert.Equal(
                "The specified boat is already booked for the specified rental date and time.",
                exception.Message
            );
        }

        [Fact]
        public async Task CreateBookingAsync_WithBatteryThatsAlreadyBookedOnSameDay_ThrowsInvalidOperationException()
        {
            // Arrange
            Boat boat = new Boat("boat1", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Battery battery = new Battery("Battery 100", BatteryStatus.Available, user);
            _dbContext.Batteries.Add(battery);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Today.AddDays(5);

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(2);

            var existingBooking = new Booking(
                null,
                battery,
                rentalDateTime.AddHours(10),
                BookingStatus.Active,
                user2,
                price
            );
            await _dbContext.Bookings.AddAsync(existingBooking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BatteryId = battery.Id,
                RentalDateTime = rentalDateTime.AddHours(15),
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CreateBookingAsync(model)
            );
            Assert.Equal(
                "The specified battery is already booked for the specified rental date.",
                exception.Message
            );
        }

        [Fact]
        public async Task GetBookingByIdAsync_ValidId_ReturnsBookingDetail()
        {
            //Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var boat = new Boat("Test Boat", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                boat,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            //Act
            var bookingDetail = await _bookingService.GetBookingByIdAsync(booking.Id);

            //Assert
            Assert.NotNull(bookingDetail);
            Assert.Equal(booking.Id, bookingDetail.Id);
            Assert.Equal(booking.RentalDateTime, bookingDetail.RentalDateTime);
            Assert.Equal(boat.Id, bookingDetail.Boat.Id);
            Assert.Equal(boat.Name, bookingDetail.Boat.Name);
        }

        [Fact]
        public async Task GetBookingByIdAsync_InvalidId_ThrowsKeyNotFoundException()
        {
            //Arrange
            var invalidId = 999;

            //Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.GetBookingByIdAsync(invalidId)
            );

            //Assert
            Assert.Equal($"Booking with ID {invalidId} was not found.", exception.Message);
        }

        [Fact]
        public async Task CancelBookingByIdAsync_ValidId()
        {
            //Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var boat = new Boat("Test Boat", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                boat,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            //Act
            await _bookingService.CancelBookingAsync(booking.Id);
            var canceledBooking = await _dbContext.Bookings.FindAsync(booking.Id);

            //Assert
            Assert.NotNull(canceledBooking);
            Assert.Equal(BookingStatus.Canceled, canceledBooking.Status);
        }

        [Fact]
        public async Task CancelBookingByIdAsync_InValidId_KeyNotFoundException()
        {
            //Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var boat = new Boat("Test Boat", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                boat,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            //Act && Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.CancelBookingAsync(999)
            );
        }

        [Fact]
        public async Task CancelBookingByIdAsync_AlreadyCanceled_ThrowsInvalidOperationException()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Canceled,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CancelBookingAsync(booking.Id)
            );

            Assert.Equal("The booking has already been canceled.", exception.Message);
        }

        [Fact]
        public async Task CancelBookingByIdAsync_InvalidId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invalidId = 999; // Assume this ID does not exist

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.CancelBookingAsync(invalidId)
            );

            Assert.Equal($"Booking with ID {invalidId} was not found.", exception.Message);
        }

        [Fact]
        public async Task GetAllBookingsAsync_ReturnsAllBookings()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var activeBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price,
                "testRemark"
            );
            var canceledBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(7),
                BookingStatus.Canceled,
                user,
                price,
                "testRemark"
            );

            await _dbContext.Bookings.AddRangeAsync(activeBooking, canceledBooking);
            await _dbContext.SaveChangesAsync();

            // Ensure bookings have IDs after saving
            var activePayment = new Payment(
                activeBooking.Id.ToString(),
                price.Amount,
                DateTime.Now,
                user.Id.ToString(),
                activeBooking.Id
            );

            var canceledPayment = new Payment(
                canceledBooking.Id.ToString(),
                price.Amount,
                DateTime.Now,
                user.Id.ToString(),
                canceledBooking.Id
            );

            await _dbContext.AddRangeAsync(activePayment, canceledPayment);
            await _dbContext.SaveChangesAsync();

            // Setup weather service mock
            _weatherService
                .Setup(s => s.FetchAndStoreWeatherDataAsync())
                .Returns(Task.CompletedTask);

            // Act
            var bookings = await _bookingService.GetAllBookingsAsync();

            // Assert
            Assert.NotNull(bookings);
            var bookingsList = bookings.ToList();
            Assert.Equal(2, bookingsList.Count);

            // Verify that bookings exist and have correct payments
            foreach (var booking in bookingsList)
            {
                Assert.NotNull(booking.Payment);
                Assert.NotNull(booking.Payment.Id);
                Assert.Equal(price.Amount, booking.Payment.Amount.Value);
            }

            // Verify the weather service was called
            _weatherService.Verify(s => s.FetchAndStoreWeatherDataAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllBookingsByUserId_ReturnsAllBookingsByUserId()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var activeBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            var canceledBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(7),
                BookingStatus.Canceled,
                user,
                price
            );
            var activeBooking2 = new Booking(
                null,
                null,
                DateTime.Now.AddDays(10),
                BookingStatus.Active,
                user2,
                price
            );
            var activeBooking3 = new Booking(
                null,
                null,
                DateTime.Now.AddDays(11),
                BookingStatus.Active,
                user2,
                price
            );

            await _dbContext.Bookings.AddRangeAsync(
                activeBooking,
                canceledBooking,
                activeBooking2,
                activeBooking3
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var bookings = await _bookingService.GetBookingsByUserIdAsync(user.Id);

            // Assert
            Assert.Equal(2, bookings.Count());
            Assert.All(bookings, booking => Assert.Equal(user.Id, booking.User!.Id));
        }

        [Fact]
        public async Task GetAllCurrentBookingsAsync_ReturnsAllCurrentBookings()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var activeBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            var canceledBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(7),
                BookingStatus.Canceled,
                user,
                price
            );
            var activeBooking2 = new Booking(
                null,
                null,
                DateTime.Now.AddDays(10),
                BookingStatus.Active,
                user,
                price
            );

            await _dbContext.Bookings.AddRangeAsync(activeBooking, canceledBooking, activeBooking2);
            await _dbContext.SaveChangesAsync();

            // Act
            var bookings = await _bookingService.GetAllCurrentBookingsAsync();

            // Assert
            Assert.Equal(2, bookings.Count());
            Assert.All(bookings, booking => Assert.True(booking.RentalDateTime >= DateTime.Now));
        }

        [Fact]
        public async Task DeleteBookingAsync_Valid_ReturnsTrue()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            // Act
            var isDeleted = await _bookingService.DeleteBookingAsync(booking.Id);

            // Assert
            Assert.True(isDeleted);
            Assert.True(booking.IsDeleted);
        }

        [Fact]
        public async Task DeleteBookingAsync_InValid_ReturnsFalse()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            // Act
            var isDeleted = await _bookingService.DeleteBookingAsync(999);

            // Assert
            Assert.False(isDeleted);
        }

        [Fact]
        public async Task UpdateBookingAsync_ValidModel_ReturnsTrue()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(10),
                UserId = user.Id,
                Status = BookingStatus.Active,
                Remark = "Updated Remark",
                PriceId = price.Id,
            };

            // Act
            var isUpdated = await _bookingService.UpdateBookingAsync(booking.Id, model);

            // Assert
            Assert.True(isUpdated);
            Assert.Equal(model.RentalDateTime, booking.RentalDateTime);
            Assert.Equal(model.Remark, booking.Remark);
        }

        [Fact]
        public async Task UpdateBookingAsync_ValidModelCancel_ReturnsTrue()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Now.AddDays(5);
            var booking = new Booking(
                null,
                null,
                rentalDateTime,
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                RentalDateTime = rentalDateTime,
                UserId = user.Id,
                Status = BookingStatus.Canceled,
                PriceId = price.Id,
            };

            // Act
            var isUpdated = await _bookingService.UpdateBookingAsync(booking.Id, model);

            // Assert
            Assert.True(isUpdated);
            Assert.Equal(BookingStatus.Canceled, booking.Status);
        }

        [Fact]
        public async Task UpdateBookingAsync_InValidBattery_KeyNotFoundException()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BatteryId = 999,
                RentalDateTime = DateTime.Now.AddDays(10),
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.UpdateBookingAsync(booking.Id, model)
            );
        }

        [Fact]
        public async Task UpdateBookingAsync_InValidBoat_KeyNotFoundException()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BoatId = 999,
                RentalDateTime = DateTime.Now.AddDays(10),
                UserId = user.Id,
                Status = BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.UpdateBookingAsync(booking.Id, model)
            );
        }
    }
}
