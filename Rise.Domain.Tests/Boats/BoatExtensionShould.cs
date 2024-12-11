using Rise.Domain.Boats;
using Shouldly;
using Xunit;

namespace Rise.Domain.Tests.Boats
{
    public class BoatExtensionShould
    {
        [Theory]
        [InlineData(BoatStatus.Available, "Beschikbaar")]
        [InlineData(BoatStatus.InRepair, "In reparatie")]
        [InlineData(BoatStatus.OutOfService, "Buiten dienst")]
        public void TranslateStatusToNL_ShouldReturnCorrectTranslation(
            BoatStatus status,
            string vertaling
        )
        {
            // Act
            var result = status.TranslateStatusToNL();

            // Assert
            result.ShouldBe(vertaling);
        }

        [Fact]
        public void TranslateStatusToNL_ShouldReturnOnbekend_ForInvalidStatus()
        {
            // Arrange
            var invalidStatus = (BoatStatus)999;

            // Act
            var result = invalidStatus.TranslateStatusToNL();

            // Assert
            result.ShouldBe("Onbekend");
        }
    }
}
