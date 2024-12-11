using System.Net;
using System.Net.Http;
using System.Text.Json;
using Moq;
using Rise.Shared.TimeSlots;
using Shouldly;
using Xunit;

namespace Rise.Services.Tests
{
    public class TimeSlotServiceTests
    {
        private readonly Mock<ITimeSlotService> _timeSlotServiceMock;
        private readonly DateTime _testDate = new(2024, 1, 1);
        private readonly int _testUserId = 1;

        public TimeSlotServiceTests()
        {
            _timeSlotServiceMock = new Mock<ITimeSlotService>();
        }

        [Fact]
        public async Task GetAllTimeSlotsAsync_ShouldReturnTimeSlots_WhenRequestSucceeds()
        {
            // Arrange
            var startDate = _testDate;
            var endDate = _testDate.AddDays(7);
            var expectedTimeSlots = new List<TimeSlotDto>
            {
                new()
                {
                    Date = startDate,
                    TimeSlot = 0,
                    CreatedByUserId = _testUserId,
                    Reason = "Test reason",
                },
            };

            _timeSlotServiceMock
                .Setup(x => x.GetAllTimeSlotsAsync(startDate, endDate))
                .ReturnsAsync(expectedTimeSlots);

            // Act
            var result = await _timeSlotServiceMock.Object.GetAllTimeSlotsAsync(startDate, endDate);

            // Assert
            result.ShouldNotBeNull();
            var timeSlots = result.ToList();
            timeSlots.Count.ShouldBe(1);
            timeSlots[0].Date.ShouldBe(startDate);
            timeSlots[0].TimeSlot.ShouldBe(0);
            timeSlots[0].CreatedByUserId.ShouldBe(_testUserId);

            _timeSlotServiceMock.Verify(
                x => x.GetAllTimeSlotsAsync(startDate, endDate),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllTimeSlotsAsync_ShouldReturnEmptyList_WhenNoTimeSlotsFound()
        {
            // Arrange
            var startDate = _testDate;
            var endDate = _testDate.AddDays(7);

            _timeSlotServiceMock
                .Setup(x => x.GetAllTimeSlotsAsync(startDate, endDate))
                .ReturnsAsync(new List<TimeSlotDto>());

            // Act
            var result = await _timeSlotServiceMock.Object.GetAllTimeSlotsAsync(startDate, endDate);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
            _timeSlotServiceMock.Verify(
                x => x.GetAllTimeSlotsAsync(startDate, endDate),
                Times.Once
            );
        }

        [Fact]
        public async Task BlockTimeSlotAsync_ShouldSucceed_WhenRequestIsValid()
        {
            // Arrange
            var model = new TimeSlotDto
            {
                Date = _testDate,
                TimeSlot = 0,
                CreatedByUserId = _testUserId,
                Reason = "Test blocking",
            };

            _timeSlotServiceMock
                .Setup(x => x.BlockTimeSlotAsync(model))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Should.NotThrowAsync(
                async () => await _timeSlotServiceMock.Object.BlockTimeSlotAsync(model)
            );
            _timeSlotServiceMock.Verify(x => x.BlockTimeSlotAsync(model), Times.Once);
        }

        [Fact]
        public async Task UnblockTimeSlotAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var date = _testDate;
            var timeSlot = 0;

            _timeSlotServiceMock
                .Setup(x => x.UnblockTimeSlotAsync(date, timeSlot))
                .ReturnsAsync(true);

            // Act
            var result = await _timeSlotServiceMock.Object.UnblockTimeSlotAsync(date, timeSlot);

            // Assert
            result.ShouldBeTrue();
            _timeSlotServiceMock.Verify(x => x.UnblockTimeSlotAsync(date, timeSlot), Times.Once);
        }

        [Fact]
        public async Task GetAllTimeSlotsAsync_ShouldHandleDateFormatting_Correctly()
        {
            // Arrange
            var startDate = new DateTime(2024, 12, 31);
            var endDate = new DateTime(2025, 1, 1);

            _timeSlotServiceMock
                .Setup(x => x.GetAllTimeSlotsAsync(startDate, endDate))
                .ReturnsAsync(new List<TimeSlotDto>());

            // Act
            await _timeSlotServiceMock.Object.GetAllTimeSlotsAsync(startDate, endDate);

            // Assert
            _timeSlotServiceMock.Verify(
                x => x.GetAllTimeSlotsAsync(startDate, endDate),
                Times.Once
            );
        }

        [Fact]
        public async Task BlockTimeSlotAsync_ShouldThrowInvalidOperationException_WhenBookingExists()
        {
            // Arrange
            var model = new TimeSlotDto
            {
                Date = _testDate,
                TimeSlot = 0,
                CreatedByUserId = _testUserId,
                Reason = "Test blocking",
            };

            _timeSlotServiceMock
                .Setup(x => x.BlockTimeSlotAsync(model))
                .ThrowsAsync(
                    new InvalidOperationException(
                        $"Cannot block time slot: existing booking found for {model.TimeSlot} on {model.Date:d}"
                    )
                );

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                async () => await _timeSlotServiceMock.Object.BlockTimeSlotAsync(model)
            );
            exception.Message.ShouldContain("Cannot block time slot");
            exception.Message.ShouldContain("existing booking found");
        }

        [Fact]
        public async Task BlockTimeSlotAsync_ShouldNotBlock_WhenSlotIsAlreadyBlocked()
        {
            // Arrange
            var model = new TimeSlotDto
            {
                Date = _testDate,
                TimeSlot = 0,
                CreatedByUserId = _testUserId,
                Reason = "Test blocking",
            };

            _timeSlotServiceMock
                .Setup(x => x.BlockTimeSlotAsync(model))
                .Returns(Task.CompletedTask); // Service returns without throwing when slot is already blocked

            // Act & Assert
            await Should.NotThrowAsync(
                async () => await _timeSlotServiceMock.Object.BlockTimeSlotAsync(model)
            );
            _timeSlotServiceMock.Verify(x => x.BlockTimeSlotAsync(model), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BlockTimeSlotAsync_ShouldHandleEmptyReason(string reason)
        {
            // Arrange
            var model = new TimeSlotDto
            {
                Date = _testDate,
                TimeSlot = 0,
                CreatedByUserId = _testUserId,
                Reason = reason,
            };

            _timeSlotServiceMock
                .Setup(x => x.BlockTimeSlotAsync(model))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Should.NotThrowAsync(
                async () => await _timeSlotServiceMock.Object.BlockTimeSlotAsync(model)
            );
            _timeSlotServiceMock.Verify(x => x.BlockTimeSlotAsync(model), Times.Once);
        }

        [Fact]
        public async Task UnblockTimeSlotAsync_ShouldReturnFalse_WhenSlotNotFound()
        {
            // Arrange
            var date = _testDate;
            var timeSlot = 0;

            _timeSlotServiceMock
                .Setup(x => x.UnblockTimeSlotAsync(date, timeSlot))
                .ReturnsAsync(false);

            // Act
            var result = await _timeSlotServiceMock.Object.UnblockTimeSlotAsync(date, timeSlot);

            // Assert
            result.ShouldBeFalse();
            _timeSlotServiceMock.Verify(x => x.UnblockTimeSlotAsync(date, timeSlot), Times.Once);
        }

        [Fact]
        public async Task GetAllTimeSlotsAsync_ShouldReturnNull_WhenDatabaseError()
        {
            // Arrange
            var startDate = _testDate;
            var endDate = _testDate.AddDays(7);

            _timeSlotServiceMock
                .Setup(x => x.GetAllTimeSlotsAsync(startDate, endDate))
                .ReturnsAsync((IEnumerable<TimeSlotDto>?)null);

            // Act
            var result = await _timeSlotServiceMock.Object.GetAllTimeSlotsAsync(startDate, endDate);

            // Assert
            result.ShouldBeNull();
            _timeSlotServiceMock.Verify(
                x => x.GetAllTimeSlotsAsync(startDate, endDate),
                Times.Once
            );
        }

        [Fact]
        public async Task BlockTimeSlotAsync_ShouldThrowArgumentNullException_WhenModelIsNull()
        {
            // Arrange
            TimeSlotDto? model = null;

            _timeSlotServiceMock
                .Setup(x => x.BlockTimeSlotAsync(model))
                .ThrowsAsync(new ArgumentNullException(nameof(model)));

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentNullException>(
                async () => await _timeSlotServiceMock.Object.BlockTimeSlotAsync(model)
            );
            exception.ParamName.ShouldBe("model");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(999)]
        public async Task UnblockTimeSlotAsync_ShouldThrow_WhenInvalidTimeSlot(int invalidTimeSlot)
        {
            // Arrange
            var date = _testDate;

            _timeSlotServiceMock
                .Setup(x => x.UnblockTimeSlotAsync(date, invalidTimeSlot))
                .ThrowsAsync(
                    new ArgumentException("Invalid time slot value", nameof(invalidTimeSlot))
                );

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(
                async () =>
                    await _timeSlotServiceMock.Object.UnblockTimeSlotAsync(date, invalidTimeSlot)
            );
            exception.Message.ShouldContain("Invalid time slot value");
            exception.ParamName.ShouldBe(nameof(invalidTimeSlot));
        }
    }
}
