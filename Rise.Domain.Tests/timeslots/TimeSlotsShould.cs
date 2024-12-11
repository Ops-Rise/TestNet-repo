using System;
using Rise.Domain.TimeSlots;
using Rise.Domain.Users;
using Shouldly;
using Xunit;

namespace Rise.Domain.Tests.TimeSlots
{
    public class TimeSlotShould
    {
        User _testUser = new User("auth0|123456", "John") { Id = 1 };

        [Fact]
        public void BeCreatedWithValidParameters()
        {
            // Arrange
            var testDate = new DateTime(2024, 1, 1);
            var testType = TimeSlot.TimeSlotType.ochtend;
            var testReason = "Maintenance";
            var testUserId = 1;
            var testCreatedAt = DateTime.UtcNow;
            var testUser = _testUser;

            // Act
            var timeSlot = new TimeSlot
            {
                Id = 1,
                Date = testDate,
                Type = testType,
                Reason = testReason,
                CreatedByUserId = testUserId,
                CreatedAt = testCreatedAt,
                User = testUser,
            };

            // Assert
            timeSlot.ShouldNotBeNull();
            timeSlot.Date.ShouldBe(testDate);
            timeSlot.Type.ShouldBe(testType);
            timeSlot.Reason.ShouldBe(testReason);
            timeSlot.CreatedByUserId.ShouldBe(testUserId);
            timeSlot.CreatedAt.ShouldBe(testCreatedAt);
            timeSlot.User.ShouldBe(testUser);
        }

        [Fact]
        public void AllowReasonToBeNullOrEmpty()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                Date = DateTime.UtcNow,
                Type = TimeSlot.TimeSlotType.ochtend,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                Reason = null,
            };

            // Act & Assert
            timeSlot.Reason.ShouldBeNull();

            // Test empty string
            timeSlot.Reason = string.Empty;
            timeSlot.Reason.ShouldBe(string.Empty);
        }

        [Theory]
        [InlineData(TimeSlot.TimeSlotType.ochtend)]
        [InlineData(TimeSlot.TimeSlotType.middag)]
        [InlineData(TimeSlot.TimeSlotType.namiddag)]
        public void BeCreatedWithValidTimeSlotType(TimeSlot.TimeSlotType type)
        {
            // Arrange & Act
            var timeSlot = new TimeSlot
            {
                Date = DateTime.UtcNow,
                Type = type,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                User = _testUser,
            };

            // Assert
            timeSlot.Type.ShouldBe(type);
        }

        [Fact]
        public void DefaultReasonShouldBeEmptyString()
        {
            // Arrange & Act
            var timeSlot = new TimeSlot
            {
                Date = DateTime.UtcNow,
                Type = TimeSlot.TimeSlotType.ochtend,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                User = _testUser,
            };

            // Assert
            timeSlot.Reason.ShouldBe(string.Empty);
        }

        [Fact]
        public void AllowUpdatingProperties()
        {
            // Arrange
            var timeSlot = new TimeSlot
            {
                Date = new DateTime(2024, 1, 1),
                Type = TimeSlot.TimeSlotType.ochtend,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                User = _testUser,
                Reason = "EERSTE REDEN",
            };

            // Act
            timeSlot.Date = new DateTime(2024, 1, 2);
            timeSlot.Type = TimeSlot.TimeSlotType.middag;
            timeSlot.Reason = "Updated REDEN";

            // Assert
            timeSlot.Date.ShouldBe(new DateTime(2024, 1, 2));
            timeSlot.Type.ShouldBe(TimeSlot.TimeSlotType.middag);
            timeSlot.Reason.ShouldBe("Updated REDEN");
        }
    }
}
