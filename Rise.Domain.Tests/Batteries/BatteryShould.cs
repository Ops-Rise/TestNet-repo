using Rise.Domain.Addresses;
using Rise.Domain.Batteries;
using Rise.Domain.Bookings;
using Rise.Domain.Users;
using Shouldly;
using Xunit;

namespace Rise.Domain.Tests.Batteries
{
    public class BatteryShould
    {
        private static readonly Address _testAddress = new Address(
            "testStreet",
            "1",
            "testCity",
            "9000"
        );

        private static readonly User _testUser =  new User("auth0|testUserId","test@gmail.com")
    {
        Firstname = "TestFirstName",
        Lastname = "TestLastName",
        BirthDay = new DateTime(1990, 1, 1), // Example birthdate
        PhoneNumber = "0477889955",
        Address = _testAddress,
        IsRegistrationComplete = true,
    };

        private readonly string _testBatteryName = "TestBattery";
        private readonly BatteryStatus _testBatteryStatus = BatteryStatus.Available;

        [Fact]
        public void BeCreated()
        {
            Battery testBattery = new Battery(_testBatteryName, _testBatteryStatus, _testUser);

            testBattery.ShouldNotBeNull();
            testBattery.Name.ShouldBe(_testBatteryName);
            testBattery.Status.ShouldBe(_testBatteryStatus);
            testBattery.User.ShouldBe(_testUser);
            testBattery.UserId.ShouldBe(_testUser.Id);
            testBattery.Bookings.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NotBeCreatedWithInvalidName(string invalidName)
        {
            Action act = () =>
            {
                Battery testBattery = new Battery(invalidName, _testBatteryStatus, _testUser);
            };

            var exception = act.ShouldThrow<ArgumentException>();
            exception.Message.ShouldContain("Name");
        }

        [Fact]
        public void NotBeCreatedWithNullUser()
        {
            // Arrange
            User nullUser = null!;

            // Act
            Action act = () =>
            {
                Battery testBattery = new Battery(_testBatteryName, _testBatteryStatus, nullUser);
            };

            // Assert
            var exception = act.ShouldThrow<ArgumentException>();
            exception.Message.ShouldBe("Value cannot be null. (Parameter 'value')");
        }

        [Fact]
        public void AllowStatusChange()
        {
            Battery testBattery = new Battery(_testBatteryName, _testBatteryStatus, _testUser);

            testBattery.Status = BatteryStatus.Reserve;

            testBattery.Status.ShouldBe(BatteryStatus.Reserve);
        }
    }
}
