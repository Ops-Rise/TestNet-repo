using Rise.Domain.Addresses;
using Shouldly;

namespace Rise.Domain.Tests.Addresses;

public class AddressShould
{
    private readonly string _testStreet = "Main St";
    private readonly string _testHouseNumber = "123";
    private readonly string? _testUnitNumber = "A";
    private readonly string _testCity = "Sample City";
    private readonly string _testPostalCode = "1234";

    [Fact]
    public void BeCreatedWithValidParameters()
    {
        Address testAddress = new Address(
            _testStreet,
            _testHouseNumber,
            _testUnitNumber,
            _testCity,
            _testPostalCode
        );

        testAddress.ShouldNotBeNull();
        testAddress.Street.ShouldBe(_testStreet);
        testAddress.HouseNumber.ShouldBe(_testHouseNumber);
        testAddress.UnitNumber.ShouldBe(_testUnitNumber);
        testAddress.City.ShouldBe(_testCity);
        testAddress.PostalCode.ShouldBe(_testPostalCode);
    }

    [Fact]
    public void BeCreatedWithUnitNumberNull()
    {
        Address testAddress = new Address(
            _testStreet,
            _testHouseNumber,
            null,
            _testCity,
            _testPostalCode
        );

        testAddress.ShouldNotBeNull();
        testAddress.UnitNumber.ShouldBeNull();
    }

    [Fact]
    public void NotBeCreatedWithNullStreet()
    {
        string testStreet = null!;

        Action act = () =>
        {
            Address testAddress = new Address(
                testStreet,
                _testHouseNumber,
                _testUnitNumber,
                _testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Street')");
    }

    [Fact]
    public void NotBeCreatedWithNullHouseNumber()
    {
        string testHouseNumber = null!;

        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                testHouseNumber,
                _testUnitNumber,
                _testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'HouseNumber')");
    }

    [Fact]
    public void NotBeCreatedWithNullCity()
    {
        string testCity = null!;

        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                _testHouseNumber,
                _testUnitNumber,
                testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'City')");
    }

    [Fact]
    public void NotBeCreatedWithNullPostalCode()
    {
        string testPostalCode = null!;

        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                _testHouseNumber,
                _testUnitNumber,
                _testCity,
                testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'PostalCode')");
    }

    [Fact]
    public void NotBeCreatedWithWhiteSpaceStreet()
    {
        string testStreet = " ";

        Action act = () =>
        {
            Address testAddress = new Address(
                testStreet,
                _testHouseNumber,
                _testUnitNumber,
                _testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Required input Street was empty. (Parameter 'Street')");
    }

    [Fact]
    public void NotBeCreatedWithWhiteSpaceHouseNumber()
    {
        string testHouseNumber = " ";

        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                testHouseNumber,
                _testUnitNumber,
                _testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "Required input HouseNumber was empty. (Parameter 'HouseNumber')"
        );
    }

    [Fact]
    public void NotBeCreatedWithWhiteSpaceCity()
    {
        string testCity = " ";

        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                _testHouseNumber,
                _testUnitNumber,
                testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Required input City was empty. (Parameter 'City')");
    }

    [Fact]
    public void NotBeCreatedWithWhiteSpacePostalCode()
    {
        string testPostalCode = " ";

        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                _testHouseNumber,
                _testUnitNumber,
                _testCity,
                testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Required input PostalCode was empty. (Parameter 'PostalCode')");
    }

    [Theory]
    [InlineData("0123")]
    [InlineData("456")]
    [InlineData("87542")]
    [InlineData("56A5")]
    public void NotBeCreatedWithInvalidPostalCode(string invalidPostcalCode)
    {
        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                _testHouseNumber,
                _testUnitNumber,
                _testCity,
                invalidPostcalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "PostalCode must be exactly 4 digits and cannot start with 0. (Parameter 'PostalCode')"
        );
    }

    [Theory]
    [InlineData("022")]
    [InlineData("a5")]
    [InlineData("x7")]
    [InlineData("001")]
    public void NotBeCreatedWithInvalidHouseNumber(string invalidHouseNumber)
    {
        Action act = () =>
        {
            Address testAddress = new Address(
                _testStreet,
                invalidHouseNumber,
                _testUnitNumber,
                _testCity,
                _testPostalCode
            );
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "HouseNumber must start with number between 1 and 9. (Parameter 'HouseNumber')"
        );
    }

    [Fact]
    public void AllowUpdatingProperties()
    {
        // Arrange
        var address = new Address
        {
            Street = _testStreet,
            HouseNumber = _testHouseNumber,
            City = _testCity,
            PostalCode = _testPostalCode,
        };

        // Act
        address.Street = "Updated St";
        address.City = "Updated City";

        // Assert
        address.Street.ShouldBe("Updated St");
        address.City.ShouldBe("Updated City");
    }

    [Fact]
    public void RejectSettingStreetToNull()
    {
        // Arrange
        var address = new Address
        {
            Street = _testStreet,
            HouseNumber = _testHouseNumber,
            City = _testCity,
            PostalCode = _testPostalCode,
        };

        // Act
        Action act = () => address.Street = null!;

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Street')");
    }

    [Fact]
    public void RejectSettingHouseNumberToWhitespace()
    {
        // Arrange
        var address = new Address
        {
            Street = _testStreet,
            HouseNumber = _testHouseNumber,
            City = _testCity,
            PostalCode = _testPostalCode,
        };

        // Act
        Action act = () => address.HouseNumber = "   ";

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "Required input HouseNumber was empty. (Parameter 'HouseNumber')"
        );
    }

    [Fact]
    public void RejectSettingPostalCodeToInvalidValue()
    {
        // Arrange
        var address = new Address
        {
            Street = _testStreet,
            HouseNumber = _testHouseNumber,
            City = _testCity,
            PostalCode = _testPostalCode,
        };

        // Act
        Action act = () => address.PostalCode = "0000";

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "PostalCode must be exactly 4 digits and cannot start with 0. (Parameter 'PostalCode')"
        );
    }

    [Fact]
    public void RejectSettingCityToEmptyString()
    {
        // Arrange
        var address = new Address
        {
            Street = _testStreet,
            HouseNumber = _testHouseNumber,
            City = _testCity,
            PostalCode = _testPostalCode,
        };

        // Act
        Action act = () => address.City = "";

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Required input City was empty. (Parameter 'City')");
    }
}
