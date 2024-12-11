using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.ProfileImages;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Server.IntegrationTests.Utils;

public class Seeder
{
    private readonly ApplicationDbContext _context;
    public static int UserId;
    public static int AdminId;
    public static int TestUserId;
    public static User Admin;
    public static int BatteryId1,
        BatteryId2;
    public static int BoatId1;

    public static int BoatId2,
        BoatId3;
    public static int BookingId1,
        BookingId7;
    public static int PriceId1;

    public static int UserProfileImageId;
    public static int AdminProfileImageId;

    public Seeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Seed()
    {
        var user = new User(TestData.Auth0UserIdFromUser, TestData.UserEmail);
        var admin = new User(TestData.Auth0UserIdFromAdmin, TestData.AdminEmail);
        var testUser = new User(TestData.Auth0UserIdFromTestUser, TestData.TestUserEmail);
        _context.Users.AddRange(user, admin, testUser);
        _context.SaveChanges();
        UserId = user.Id;
        AdminId = admin.Id;
        TestUserId = testUser.Id;

        var battery1 = new Battery(TestData.BatteryName1, BatteryStatus.Available, user);
        var battery2 = new Battery(TestData.BatteryName2, BatteryStatus.Available, user);
        var battery3 = new Battery(TestData.BatteryName3, BatteryStatus.Reserve, user);
        var battery4 = new Battery(TestData.BatteryName4, BatteryStatus.OutOfService, user);
        var battery5 = new Battery(TestData.BatteryName5, BatteryStatus.InRepair, user);
        _context.Batteries.AddRange(battery1, battery2, battery3, battery4, battery5);
        _context.SaveChanges();

        BatteryId1 = battery1.Id;
        BatteryId2 = battery2.Id;

        var boat1 = new Boat(TestData.BoatName1, BoatStatus.Available);
        var boat2 = new Boat(TestData.BoatName2, BoatStatus.Available);
        var boat3 = new Boat(TestData.BoatName3, BoatStatus.InRepair);

        _context.Boats.AddRange(boat1, boat2, boat3);
        _context.SaveChanges();

        BoatId1 = boat1.Id;
        BoatId2 = boat2.Id;
        BoatId3 = boat3.Id;

        Price price1 = new Price((decimal)20.99) { CreatedAt = DateTime.Now.AddDays(-5) };
        Price price2 = new Price((decimal)30) { CreatedAt = DateTime.Now.AddDays(5) };
        Price price3 = new Price((decimal)45)
        {
            CreatedAt = DateTime.Now.AddDays(-1),
            IsDeleted = true,
        };
        Price price4 = new Price((decimal)49.99) { CreatedAt = DateTime.Now.AddDays(1) };

        _context.Prices.AddRange(price1, price2, price3, price4);
        _context.SaveChanges();
        PriceId1 = price1.Id;

        Booking booking1 = new Booking(
            boat1,
            battery1,
            TestData.NowPlus5Days,
            BookingStatus.Active,
            user,
            price1,
            "testRemark"
        );
        Booking booking2 = new Booking(
            boat1,
            battery1,
            TestData.NowPlus18Days,
            BookingStatus.Canceled,
            user,
            price1
        );
        Booking booking3 = new Booking(
            boat1,
            battery1,
            TestData.NowPlus6Days,
            BookingStatus.Active,
            user,
            price1
        );
        Booking booking4 = new Booking(
            boat2,
            battery2,
            TestData.NowPlus5Days,
            BookingStatus.Active,
            user,
            price1
        );
        Booking booking5 = new Booking(
            boat2,
            battery2,
            TestData.NowPlus15Days,
            BookingStatus.Active,
            user,
            price1
        );
        Booking booking6 = new Booking(
            boat2,
            battery2,
            TestData.NowPlus7Days,
            BookingStatus.Active,
            user,
            price1
        );
        Booking booking7 = new Booking(
            boat2,
            battery2,
            TestData.NowPlus7Days,
            BookingStatus.Active,
            admin,
            price1
        );
        _context.Bookings.AddRangeAsync(
            booking1,
            booking2,
            booking3,
            booking4,
            booking5,
            booking6,
            booking7
        );
        _context.SaveChanges();
        BookingId1 = booking1.Id;
        BookingId7 = booking7.Id;

        var userProfileImage = new ProfileImage(user.Id, new byte[] { 1, 2, 3, 4, 5 }, "image/png");
        _context.ProfileImages.Add(userProfileImage);
        _context.SaveChanges();
        UserProfileImageId = userProfileImage.Id;

        var adminProfileImage = new ProfileImage(
            admin.Id,
            new byte[] { 6, 7, 8, 9, 10 },
            "image/png"
        );
        _context.ProfileImages.Add(adminProfileImage);
        _context.SaveChanges();
        AdminProfileImageId = adminProfileImage.Id;
    }
}
