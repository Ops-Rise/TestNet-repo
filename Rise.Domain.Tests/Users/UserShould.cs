using Rise.Domain.Addresses;
using Rise.Domain.Users;
using Shouldly;

namespace Rise.Domain.Tests.Users;

public class UserShould
{
    private readonly string _testAuth0UserId = "testAuth0UserId";
    private readonly string _testEmail = "test@example.com";
    private readonly string _testFirstname = "John";
    private readonly string _testLastname = "Doe";
    private readonly DateTime? _testBirthDay = new DateTime(1990, 1, 1);
    private readonly string _testPhoneNumber = "0498998877";
    private readonly bool _testIsRegistrationComplete = false;
    private readonly bool _testIsTrainingComplete = false;

    [Fact]
    public void BeCreated()
    {
        User testUser = new User(_testAuth0UserId, _testEmail)
        {
            Firstname = _testFirstname,
            Lastname = _testLastname,
            BirthDay = _testBirthDay,
            PhoneNumber = _testPhoneNumber,
            IsRegistrationComplete = _testIsRegistrationComplete,
            IsTrainingComplete = false,
        };

        testUser.ShouldNotBeNull();
        testUser.Auth0UserId.ShouldBe(_testAuth0UserId);
        testUser.Email.ShouldBe(_testEmail);
        testUser.Firstname.ShouldBe(_testFirstname);
        testUser.Lastname.ShouldBe(_testLastname);
        testUser.BirthDay.ShouldBe(_testBirthDay);
        testUser.PhoneNumber.ShouldBe(_testPhoneNumber);
        testUser.IsRegistrationComplete.ShouldBe(_testIsRegistrationComplete);
        testUser.IsTrainingComplete.ShouldBe(_testIsTrainingComplete);
    }

    [Fact]
    public void NotBeCreatedWithNullAuth0UserId()
    {
        string testAuth0UserId = null!;

        Action act = () =>
        {
            User testUser = new User(testAuth0UserId, _testEmail);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Auth0UserId')");
    }

    [Fact]
    public void NotBeCreatedWithNullFirstname()
    {
        string testFirstname = null!;

        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, _testEmail);
            testUser.Firstname = testFirstname;
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Firstname')");
    }

    [Fact]
    public void NotBeCreatedWithNullLastname()
    {
        string testLastname = null!;

        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, _testEmail);
            testUser.Lastname = testLastname;
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Lastname')");
    }

    [Fact]
    public void NotBeCreatedWithNullPhoneNumber()
    {
        string testPhoneNumber = null!;

        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, _testEmail);
            testUser.PhoneNumber = testPhoneNumber;
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'input')");
    }

    [Fact]
    public void NotBeCreatedWithNullEmail()
    {
        string testEmail = null!;

        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, testEmail);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Email')");
    }

    [Fact]
    public void NotBeCreatedWithWhiteSpaceAuth0UserId()
    {
        string testAuth0UserId = " ";

        Action act = () =>
        {
            User testUser = new User(testAuth0UserId, _testEmail);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "Required input Auth0UserId was empty. (Parameter 'Auth0UserId')"
        );
    }

    [Fact]
    public void NotBeCreatedWithWhiteSpaceEmail()
    {
        string testEmail = " ";

        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, testEmail);
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Required input Email was empty. (Parameter 'Email')");
    }

    [Fact]
    public void NotBeCreatedWithNullAddress()
    {
        Address testAddress = null!;

        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, _testEmail);
            testUser.Address = testAddress;
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe("Value cannot be null. (Parameter 'Address')");
    }

    [Fact]
    public void AllowSettingValidProperties()
    {
        User testUser = new User(_testAuth0UserId, _testEmail)
        {
            Firstname = _testFirstname,
            Lastname = _testLastname,
            BirthDay = _testBirthDay,
            PhoneNumber = _testPhoneNumber,
            IsRegistrationComplete = true,
            IsTrainingComplete = true,
        };

        testUser.Firstname.ShouldBe(_testFirstname);
        testUser.Lastname.ShouldBe(_testLastname);
        testUser.BirthDay.ShouldBe(_testBirthDay);
        testUser.PhoneNumber.ShouldBe(_testPhoneNumber);
        testUser.IsRegistrationComplete.ShouldBe(true);
        testUser.IsTrainingComplete.ShouldBe(true);
    }

    [Theory]
    [InlineData("04989988771")]
    [InlineData("0398225544")]
    [InlineData("+324975544111")]
    [InlineData("+4245556677")]
    [InlineData("++32497445533")]
    public void NotBeCreatedWithInvalidPhoneNumber(string invalidPhoneNumber)
    {
        Action act = () =>
        {
            User testUser = new User(_testAuth0UserId, _testEmail)
            {
                Firstname = _testFirstname,
                Lastname = _testLastname,
                BirthDay = _testBirthDay,
                PhoneNumber = invalidPhoneNumber,
                IsRegistrationComplete = true,
                IsTrainingComplete = false,
            };
        };

        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldBe(
            "PhoneNumber must be Belgian format (+32 or 04) (Parameter 'PhoneNumber')"
        );
    }

    [Theory]
    [InlineData("0455778899")]
    [InlineData("+32477889955")]
    [InlineData("0477541235")]
    public void BeCreatedWithValidPhoneNumber(string validPhoneNumber)
    {
        User testUser = new User(_testAuth0UserId, _testEmail)
        {
            Firstname = _testFirstname,
            Lastname = _testLastname,
            BirthDay = _testBirthDay,
            PhoneNumber = validPhoneNumber,
            IsRegistrationComplete = true,
            IsTrainingComplete = false,
        };
        testUser.PhoneNumber.ShouldBe(validPhoneNumber);
    }
}
