using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.Batteries;
using Rise.Shared.Batteries;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Emails;
using Rise.Shared.Emails.Models;
using Rise.Shared.Emails.Models;
using Xunit;

namespace Rise.Services.Tests.Batteries
{
    public class BatteryAndBoatAssignmentProcessorTests
    {
        private readonly TestLoggerProvider _loggerProvider;
        private readonly ILogger<BatteryAndBoatAssignmentProcessor> _logger;
        private readonly Mock<IBatteryService> _batteryServiceMock;
        private readonly Mock<IBookingService> _bookingServiceMock;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
        private readonly ApplicationDbContext _dbContext;
        private readonly BatteryAndBoatAssignmentProcessor _processor;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;

        public BatteryAndBoatAssignmentProcessorTests()
        {
            _loggerProvider = new TestLoggerProvider();
            _logger = _loggerProvider.CreateLogger<BatteryAndBoatAssignmentProcessor>();
            _batteryServiceMock = new Mock<IBatteryService>();
            _bookingServiceMock = new Mock<IBookingService>();
            _emailTemplateServiceMock = new Mock<IEmailTemplateService>();
            _emailServiceMock = new Mock<IEmailService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("BatteryAssignmentTestDb")
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _processor = new BatteryAndBoatAssignmentProcessor(
                _logger,
                _dbContext,
                _batteryServiceMock.Object,
                _bookingServiceMock.Object,
                _emailServiceMock.Object,
                _emailTemplateServiceMock.Object
            );
        }

        private List<BatteryDto.BatteryDetail> _batteries = new List<BatteryDto.BatteryDetail>
        {
            new BatteryDto.BatteryDetail
            {
                Id = 1,
                Status = BatteryStatus.InRepair,
                Name = "Battery A",
                DateLastUsed = DateTime.Today.AddDays(-5),
                UseCycles = 10,
            },
            new BatteryDto.BatteryDetail
            {
                Id = 2,
                Status = BatteryStatus.Available,
                Name = "Battery B",
                DateLastUsed = DateTime.Today.AddDays(-3),
                UseCycles = 15,
            },
            new BatteryDto.BatteryDetail
            {
                Id = 3,
                Status = BatteryStatus.Reserve,
                Name = "Battery C",
                DateLastUsed = DateTime.Today.AddDays(-1),
                UseCycles = 5,
            },
            new BatteryDto.BatteryDetail
            {
                Id = 4,
                Status = BatteryStatus.Available,
                Name = "Battery D",
                DateLastUsed = DateTime.Today.AddDays(-2),
                UseCycles = 7,
            },
            new BatteryDto.BatteryDetail
            {
                Id = 5,
                Status = BatteryStatus.Available,
                Name = "Battery D",
                DateLastUsed = DateTime.Today.AddDays(-2),
                UseCycles = 6,
            },
        };

        private User _testUser = new User("auth0UserId", "testuser@example.com");
        private Boat _testBoat = new Boat("Test Boat", BoatStatus.Available);
        private Boat _boat2 = new Boat("Boat2", BoatStatus.Available);
        private Boat _boat3 = new Boat("Boat3", BoatStatus.Available);
        private Boat _boat4 = new Boat("Boat4", BoatStatus.InRepair);
        private Battery _testBattery = new Battery(
            "Test Battery",
            BatteryStatus.Available,
            new User("auth0UserId", "testuser@example.com")
        );
        private Price _testPrice = new Price(10m);

        [Fact]
        public async Task ProcessBatteryAndBoatAssignmentsAsync_ShouldAssignBatteryToBooking()
        {
            // Arrange
            _dbContext.Database.EnsureDeleted();
            var booking = new Booking(
                _testBoat,
                null,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            );

            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();

            var bookingId = booking.Id;

            var availableBattery = new BatteryDto.BatteryDetail
            {
                Id = 1,
                Status = BatteryStatus.Available,
                Name = "Battery 1",
                DateLastUsed = DateTime.Today.AddDays(-1),
                UseCycles = 5,
            };

            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(new List<BatteryDto.BatteryDetail> { availableBattery });

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert that the booking was updated with the assigned battery
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking.Id,
                        It.Is<BookingDto.Mutate>(m => m.BatteryId == 1)
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ProcessBatteryAndBoatAssignmentsAsync_ShouldCancelBookingIfNoBatteryAvailable()
        {
            // Arrange
            _dbContext.Database.EnsureDeleted();
            var booking = new Booking(
                _testBoat,
                null,
                DateTime.Today.AddDays(3), // Booking date within the next 3 days
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BoatId = _testBoat.Id,
                UserId = _testUser.Id,
            };
            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();

            // Mock no available batteries
            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(new List<BatteryDto.BatteryDetail>());

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking.Id,
                        It.Is<BookingDto.Mutate>(m => m.Status == BookingStatus.Canceled)
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ProcessBatteryAndBoatAssignmentsAsync_ShouldAssignBatteryBasedOnAvailabilityAndUseCycles()
        {
            // Arrange
            _dbContext.Database.EnsureDeleted();
            var booking = new Booking(
                _testBoat,
                null,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BoatId = _testBoat.Id,
                UserId = _testUser.Id,
            };
            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();

            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(_batteries);

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking.Id,
                        It.Is<BookingDto.Mutate>(m => m.BatteryId == 2) // Battery B should be selected
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task AssignBoatToBooking_NoAvailableBoats_CancelsBooking()
        {
            //Arrange
            _dbContext.Database.EnsureDeleted();
            var booking = new Booking(
                null,
                _testBattery,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();
            //only one boat in repair in db
            _dbContext.Boats.Add(_boat4);
            await _dbContext.SaveChangesAsync();

            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(_batteries);

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking.Id,
                        It.Is<BookingDto.Mutate>(m => m.Status == BookingStatus.Canceled)
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task AssignBoatToBooking_AssignsBoatWithLongestIdleTime()
        {
            //Arrange
            _dbContext.Database.EnsureDeleted();
            _dbContext.Boats.AddRange(_boat2, _boat3, _boat4);
            await _dbContext.SaveChangesAsync();
            Battery battery = _testBattery;
            _dbContext.Batteries.Add(battery);
            await _dbContext.SaveChangesAsync();
            var booking1 = new Booking(
                _boat2,
                _testBattery,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };

            var booking2 = new Booking(
                _boat3,
                _testBattery,
                DateTime.Today.AddDays(4),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            var booking3 = new Booking(
                _boat2,
                _testBattery,
                DateTime.Today.AddDays(5),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            var booking4 = new Booking( //no boat
                null,
                _testBattery,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };

            _dbContext.Bookings.AddRange(booking1, booking2, booking3, booking4);
            await _dbContext.SaveChangesAsync();
            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(_batteries);

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert that booking 4 was updated with the assigned boat
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking4.Id,
                        It.Is<BookingDto.Mutate>(m => m.BoatId == _boat3.Id)
                    ),
                Times.Once
            );
            //No update for booking2
            _bookingServiceMock.Verify(
                service => service.UpdateBookingAsync(booking2.Id, It.IsAny<BookingDto.Mutate>()),
                Times.Never
            );
        }

        [Fact]
        public async Task AssignBoatToBooking_AssignsBoatWithNoBooking()
        {
            //Arrange
            _dbContext.Database.EnsureDeleted();
            _dbContext.Boats.AddRange(_boat2, _boat3, _boat4);
            await _dbContext.SaveChangesAsync();
            Battery battery = _testBattery;
            _dbContext.Batteries.Add(battery);
            await _dbContext.SaveChangesAsync();
            var booking1 = new Booking(
                _boat3,
                _testBattery,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };

            var booking2 = new Booking(
                _boat3,
                _testBattery,
                DateTime.Today.AddDays(4),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            var booking3 = new Booking(
                _boat3,
                _testBattery,
                DateTime.Today.AddDays(5),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            var booking4 = new Booking( //no boat
                null,
                _testBattery,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            _dbContext.Bookings.AddRange(booking1, booking2, booking3, booking4);
            await _dbContext.SaveChangesAsync();
            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(_batteries);

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert that booking 4 was updated with the assigned boat
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking4.Id,
                        It.Is<BookingDto.Mutate>(m => m.BoatId == _boat2.Id)
                    ),
                Times.Once
            );
            //No update for booking2
            _bookingServiceMock.Verify(
                service => service.UpdateBookingAsync(booking2.Id, It.IsAny<BookingDto.Mutate>()),
                Times.Never
            );
        }

        [Fact]
        public async Task AssignBatteryAndBoatToBooking_AssignsBoatWithLongestIdleTimeAndCorrectBattery()
        {
            //Arrange
            _dbContext.Database.EnsureDeleted();
            _dbContext.Boats.AddRange(_boat2, _boat3, _boat4);
            await _dbContext.SaveChangesAsync();
            Battery battery = _testBattery;
            _dbContext.Batteries.Add(battery);
            await _dbContext.SaveChangesAsync();
            var booking1 = new Booking(
                _boat2,
                _testBattery,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };

            var booking2 = new Booking(
                _boat3,
                _testBattery,
                DateTime.Today.AddDays(4),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            var booking3 = new Booking(
                _boat2,
                _testBattery,
                DateTime.Today.AddDays(5),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BatteryId = _testBattery.Id,
                UserId = _testUser.Id,
            };
            var booking4 = new Booking( //no boat // no battery
                null,
                null,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                UserId = _testUser.Id,
            };
            _dbContext.Bookings.AddRange(booking1, booking2, booking3, booking4);
            await _dbContext.SaveChangesAsync();

            var availableBattery = new BatteryDto.BatteryDetail
            {
                Id = 1,
                Status = BatteryStatus.Available,
                Name = "Battery 1",
                DateLastUsed = DateTime.Today.AddDays(-1),
                UseCycles = 5,
            };

            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ReturnsAsync(new List<BatteryDto.BatteryDetail> { availableBattery });

            _bookingServiceMock
                .Setup(service =>
                    service.UpdateBookingAsync(It.IsAny<int>(), It.IsAny<BookingDto.Mutate>())
                )
                .ReturnsAsync(true);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert that booking 4 was updated with the assigned boat and battery
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking4.Id,
                        It.Is<BookingDto.Mutate>(m => m.BoatId == _boat3.Id)
                    ),
                Times.Once
            );
            _bookingServiceMock.Verify(
                service =>
                    service.UpdateBookingAsync(
                        booking4.Id,
                        It.Is<BookingDto.Mutate>(m => m.BatteryId == availableBattery.Id)
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ProcessBatteryAndBoatAssignmentsAsync_ShouldLogError_WhenFetchingBookingsFails()
        {
            // Arrange
            var booking = new Booking(
                _testBoat,
                null,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BoatId = _testBoat.Id,
                UserId = _testUser.Id,
            };

            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();
            var expectedError = new Exception("Database fetch error");
            _batteryServiceMock
                .Setup(service => service.GetAllBatteriesWithDetailsAsync())
                .ThrowsAsync(expectedError);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert
            var errorLog = _loggerProvider.Logs;
            Assert.Contains(
                errorLog,
                log =>
                    log.Level == LogLevel.Information
                    && log.Message.Contains("Processing battery assignments")
            );
            Assert.Contains(
                errorLog,
                log =>
                    log.Level == LogLevel.Error
                    && log.Message.Contains("Error occurred while fetching available batteries")
            );
            Assert.Contains(errorLog, log => log.Exception == expectedError);
        }

        [Fact]
        public async Task ProcessBatteryAndBoatAssignmentsAsync_ShouldLogError_WhenBatteryServiceFails()
        {
            // Arrange
            var booking = new Booking(
                _testBoat,
                null,
                DateTime.Today.AddDays(3),
                BookingStatus.Active,
                _testUser,
                _testPrice
            )
            {
                BoatId = _testBoat.Id,
                UserId = _testUser.Id,
            };

            _dbContext.Bookings.Add(booking);
            await _dbContext.SaveChangesAsync();
            var expectedError = new Exception("Battery service error");
            _batteryServiceMock
                .Setup(s => s.GetAllBatteriesWithDetailsAsync())
                .ThrowsAsync(expectedError);

            // Act
            await _processor.ProcessBatteryAndBoatAssignmentsAsync();

            // Assert
            var errorLog = _loggerProvider.Logs;

            Assert.Contains(
                errorLog,
                log =>
                    log.Level == LogLevel.Information
                    && log.Message.Contains("Processing battery assignments")
            );
            Assert.Contains(
                errorLog,
                log =>
                    log.Level == LogLevel.Error
                    && log.Message.Contains("Error occurred while fetching available batteries")
            );
            Assert.Contains(
                errorLog,
                log => log.Level == LogLevel.Error && log.Exception == expectedError
            );
        }

        public class TestLoggerProvider : ILoggerProvider
        {
            private readonly List<LogEntry> _logs = new();
            public IReadOnlyList<LogEntry> Logs => _logs;

            public ILogger<T> CreateLogger<T>() => new TestLogger<T>(_logs);

            public ILogger CreateLogger(string categoryName) => new TestLogger<object>(_logs);

            public void Dispose() { }
        }

        public class TestLogger<T> : ILogger<T>
        {
            private readonly List<LogEntry> _logs;

            public TestLogger(List<LogEntry> logs) => _logs = logs;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter
            )
            {
                _logs.Add(new LogEntry(logLevel, formatter(state, exception), exception));
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        }

        public record LogEntry(LogLevel Level, string Message, Exception Exception = null);

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose() { }
        }
    }
}
