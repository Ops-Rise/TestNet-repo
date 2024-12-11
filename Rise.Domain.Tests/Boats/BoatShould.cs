using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Shouldly;
using Xunit;

namespace Rise.Domain.Tests.Boats
{
    public class BoatShould
    {
        private readonly string _testBoatName = "TestBoat";
        private readonly BoatStatus _testBoatStatus = BoatStatus.Available;

        [Fact]
        public void BeCreated()
        {
            Boat testBoat = new Boat(_testBoatName, _testBoatStatus);

            testBoat.ShouldNotBeNull();
            testBoat.Name.ShouldBe(_testBoatName);
            testBoat.Status.ShouldBe(_testBoatStatus);
            testBoat.Bookings.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NotBeCreatedWithInvalidName(string invalidName)
        {
            Action act = () =>
            {
                Boat testBoat = new Boat(invalidName, _testBoatStatus);
            };

            var exception = act.ShouldThrow<ArgumentException>();
            exception.Message.ShouldContain("Name");
        }

        [Fact]
        public void AllowStatusChange()
        {
            Boat testBoat = new Boat(_testBoatName, _testBoatStatus);

            testBoat.Status = BoatStatus.Available;

            testBoat.Status.ShouldBe(BoatStatus.Available);
        }
    }
}
