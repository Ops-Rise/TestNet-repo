using Rise.Shared.Addresses;
using Rise.Shared.Users;

namespace Rise.Server.IntegrationTests.Utils;

public static class TestData
{
    public const string UserEmail = "user@test.com";
    public const string UserPassword = "User123!";
    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Admin123!";
    public const string TestUserEmail = "test@test.com";
    public const string TestUserPassword = "Test123!";
    public const string InvalidEmail = "invalidemail@";
    public static string UnknownAuth0UserId => "auth0|new-" + Guid.NewGuid(); // Unieke waarde voor onbekende gebruiker
    public const string Auth0UserIdFromUser = "auth0|67211fd379a76c27c951c701";
    public const string Auth0UserIdFromAdmin = "auth0|67211f675cee3153df558def";
    public const string Auth0UserIdFromTestUser = "auth0|67211f675cee2978df558def";
    public const string Firstname = "John";
    public const string Lastname = "Doe";
    public const string PhoneNumber = "0499887766";
    public const string InvalidPhoneNumber = "1234567890";
    public static DateTime BirthDay = new DateTime(1990, 1, 1);
    public const string Street = "Mainstreet";
    public const string HouseNumber = "1";
    public const string PostalCode = "1000";
    public const string City = "Brussels";
    public const string UnitNumber = "A";

    public const string BatteryName1 = "Battery 1";
    public const string BatteryName2 = "Battery 2";
    public const string BatteryName3 = "Battery 3";
    public const string BatteryName4 = "Battery 4";
    public const string BatteryName5 = "Battery 5";

    public const string BoatName1 = "Boat 1";
    public const string BoatName2 = "Boat 2";
    public const string BoatName3 = "Boat 3";

    public static readonly DateTime NowPlus5Days = DateTime.Now.AddDays(5);
    public static readonly DateTime NowPlus6Days = DateTime.Now.AddDays(6);
    public static readonly DateTime NowPlus7Days = DateTime.Now.AddDays(7);
    public static readonly DateTime NowPlus15Days = DateTime.Now.AddDays(15);
    public static readonly DateTime NowPlus18Days = DateTime.Now.AddDays(18);

    public static readonly DateTime NowPlus20Days = DateTime.Now.AddDays(20);
}
