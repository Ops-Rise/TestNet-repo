using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Server.IntegrationTests.Utils;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Users;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class BookingControllersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public BookingControllersTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        ResetDatabaseWithSeed();

        if (_client == null)
        {
            factory.ClientOptions.BaseAddress = new Uri("https://localhost:5001/api/");
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Add("X-Redirect-Base", "http://localhost:5001/test");
        }
        _auth0Settings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("Auth0");
    }

    private async Task<string> GetTokenAsync(Role role)
    {
        //tijdelijke zelgemaakte tokens tijdens schrijven van testen
        // _userToken =
        //_adminToken =
        // Check if the token is already retrieved and cached
        if (role == Role.ADMIN && !string.IsNullOrEmpty(_adminToken))
        {
            return _adminToken;
        }
        else if (role == Role.USER && !string.IsNullOrEmpty(_userToken))
        {
            return _userToken;
        }

        // Otherwise, fetch a new token and cache it
        var auth0Client = new AuthenticationApiClient(_auth0Settings["Domain"]);
        var tokenRequest = new ResourceOwnerTokenRequest
        {
            ClientId = _auth0Settings["ClientId"],
            ClientSecret = _auth0Settings["ClientSecret"],
            Audience = _auth0Settings["Audience"],
            Username = role == Role.ADMIN ? TestData.AdminEmail : TestData.UserEmail,
            Password = role == Role.ADMIN ? TestData.AdminPassword : TestData.UserPassword,
            Scope = "openid profile email roles",
        };

        var tokenResponse = await auth0Client.GetTokenAsync(tokenRequest);
        string token = tokenResponse.AccessToken;

        // Cache the token for other tests, so we don't have to request a new token for each test
        if (role == Role.ADMIN)
        {
            _adminToken = token;
        }
        else
        {
            _userToken = token;
        }

        return token;
    }

    private async Task SetAuthorizationHeader(Role role)
    {
        string token = await GetTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
    }

    private void ResetDatabaseWithSeed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Verwijder de database
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Voeg de seeddata toe
            var seeder = new Seeder(db);
            seeder.Seed();
        }
    }

    private void ResetDatabaseWithoutSeed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Verwijder de database
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }

    [Fact]
    public async Task CreateBookingAsyncAsAdmin_WithValidModel_ShouldReturnCreated()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.ADMIN);

        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId1,
            BatteryId = Seeder.BatteryId2,
            RentalDateTime = TestData.NowPlus20Days,
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "booking")
        {
            Content = JsonContent.Create(model),
        };
        request.Headers.Add("X-Redirect-Base", "dummyPayment");

        var response = await _client.SendAsync(request);

        // Get detailed error message
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert with detailed error message on failure
        response.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            $"Expected Created but got {response.StatusCode}. Error details: {errorContent}"
        );
    }

    [Fact]
    public async Task CreateBookingAsyncAsAdmin_OnFullyBookedTimeSlot_BadRequest()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.ADMIN);
        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId1,
            BatteryId = Seeder.BatteryId1,
            RentalDateTime = TestData.NowPlus5Days, //fully booked
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var response = await _client.PostAsJsonAsync("booking", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBookingAsyncAsUser_WithValidModel_ShouldReturnCreated()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.USER);
        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId2,
            BatteryId = Seeder.BatteryId2,
            RentalDateTime = TestData.NowPlus20Days,
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "booking")
        {
            Content = JsonContent.Create(model),
        };
        request.Headers.Add("X-Redirect-Base", "dummyPayment");

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBookingAsyncAsUser_WithValidModelFromOtherUser_Forbidden()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.USER);
        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId1,
            BatteryId = Seeder.BatteryId1,
            RentalDateTime = TestData.NowPlus5Days,
            Status = BookingStatus.Active,
            UserId =
                555 //other user id
            ,
            PriceId = Seeder.PriceId1,
        };

        // Act
        var response = await _client.PostAsJsonAsync("booking", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBookingAsyncAsAdmin_WithInValidModel_BadRequest()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.ADMIN);
        var model = new BookingDto.Mutate
        {
            BoatId = 999, //non existing boat id
            BatteryId = Seeder.BatteryId1,
            RentalDateTime = TestData.NowPlus15Days,
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var response = await _client.PostAsJsonAsync("booking", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBookingByIdAsyncAsAdmin_WithValidId_ShouldReturnBooking()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.GetFromJsonAsync<BookingDto.Detail>($"booking/{bookingId}");

        // Assert
        response.Should().NotBeNull();
        response.Id.ShouldBe(bookingId);
        response.Boat.Should().NotBeNull();
        response.Battery.Should().NotBeNull();
        response.User.Should().NotBeNull();
        response.Price.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsyncAsUser_WithValidIdFromOwnBooking_ShouldReturnBooking()
    {
        // Arrange
        ResetDatabaseWithSeed();
        await SetAuthorizationHeader(Role.USER);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.GetFromJsonAsync<BookingDto.Detail>($"booking/{bookingId}");

        // Assert
        response.Should().NotBeNull();
        response.Id.ShouldBe(bookingId);
        response.Boat.Should().NotBeNull();
        response.Battery.Should().NotBeNull();
        response.User.Should().NotBeNull();
        response.Price.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsyncAsUser_WithValidIdFromNotOwnBooking_Forbidden()
    {
        // Arrange

        await SetAuthorizationHeader(Role.USER);
        int bookingId = Seeder.BookingId7;

        // Act
        var response = await _client.GetAsync($"booking/{bookingId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingByIdAsyncAsUser_WithInValidId_NotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync($"booking/{999}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelBookingAsyncAsAdmin_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.PutAsync($"booking/cancel/{bookingId}", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canceledbooking = db.Bookings.Find(bookingId);
            canceledbooking.Status.ShouldBe(BookingStatus.Canceled);
        }
    }

    [Fact]
    public async Task CancelBookingAsyncAsUser_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.PutAsync($"booking/cancel/{bookingId}", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canceledbooking = db.Bookings.Find(bookingId);
            canceledbooking.Status.ShouldBe(BookingStatus.Canceled);
        }
    }

    [Fact]
    public async Task CancelBookingAsyncAsUser_WithValidIdFromNotOwnBooking_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        int bookingId = Seeder.BookingId7;

        // Act
        var response = await _client.PutAsync($"booking/cancel/{bookingId}", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelBookingAsyncAsAdmin_WithInValidId_NotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.PutAsync($"booking/cancel/{999}", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBookingsAsyncAsAdmin_ShouldReturnAllBookings()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetAsync("booking");
        var bookingList = await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(bookingList);
        bookingList.Should().HaveCount(7);
        bookingList.Should().Contain(b => b.Id == Seeder.BookingId7);
    }

    [Fact]
    public async Task GetAllBookingsAsyncAsAdmin_EmptyDatabase_ShouldReturnAllBookings()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        ResetDatabaseWithoutSeed();

        // Act
        var response = await _client.GetAsync("booking");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBookingsAsyncAsUser_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync("booking");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBookingsByUserIdAsyncAsAdmin_WithValidUserId_ShouldReturnAllBookings()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int userId = Seeder.UserId;

        // Act
        var response = await _client.GetAsync($"booking/user/{userId}");
        var bookingList = await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(bookingList);
        bookingList.Should().HaveCount(6);
        bookingList.Should().Contain(b => b.Id == Seeder.BookingId1);
    }

    [Fact]
    public async Task GetAllBookingsByUserIdAsyncAsAdmin_WithInValidUserId_NotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int userId = 999;

        // Act
        var response = await _client.GetAsync($"booking/user/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBookingsByUserIdAsyncAsUser_WithOwnUserId_ShouldReturnAllBookings()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        int userId = Seeder.UserId;

        // Act
        var response = await _client.GetAsync($"booking/user/{userId}");
        var bookingList = await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(bookingList);
        bookingList.Should().HaveCount(6);
        bookingList.Should().Contain(b => b.Id == Seeder.BookingId1);
    }

    [Fact]
    public async Task GetAllBookingsByUserIdAsyncAsUser_WithNotOwnUserId_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        int userId = Seeder.AdminId;

        // Act
        var response = await _client.GetAsync($"booking/user/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentBookingsAsyncAsAdmin_ShouldReturnAllCurrentBookings()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetAsync("booking/current");
        var bookingList = await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(bookingList);
        bookingList.Should().HaveCount(6);
        bookingList.Should().Contain(b => b.Id == Seeder.BookingId1);
    }

    [Fact]
    public async Task GetCurrentBookingsAsyncAsUser_ShouldReturnAllCurrentBookings()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync("booking/current");
        var bookingList = await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(bookingList);
        bookingList.Should().HaveCount(6);
        bookingList.Should().Contain(b => b.Id == Seeder.BookingId1);
    }

    [Fact]
    public async Task DeleteBookingAsyncAsAdmin_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.DeleteAsync($"booking/{bookingId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deletedBooking = db.Bookings.Find(bookingId);
            Assert.True(deletedBooking.IsDeleted);
        }
    }

    [Fact]
    public async Task DeleteBookingAsyncAsAdmin_WithInValidId_NotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = 9991;

        // Act
        var response = await _client.DeleteAsync($"booking/{bookingId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBookingAsyncAsUser_WithValidId_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        int bookingId = Seeder.BookingId1;

        // Act
        var response = await _client.DeleteAsync($"booking/{bookingId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateBookingAsyncAsAdmin_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = Seeder.BookingId1;
        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId1,
            BatteryId = Seeder.BatteryId2,
            RentalDateTime = TestData.NowPlus15Days,
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var response = await _client.PutAsJsonAsync($"booking/{bookingId}", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedBooking = db.Bookings.Find(bookingId);
            updatedBooking.BoatId.ShouldBe(model.BoatId);
            updatedBooking.BatteryId.ShouldBe(model.BatteryId);
            updatedBooking.RentalDateTime.ShouldBe(model.RentalDateTime);
            updatedBooking.UserId.ShouldBe(model.UserId);
            updatedBooking.PriceId.ShouldBe(model.PriceId);
        }
    }

    [Fact]
    public async Task UpdateBookingAsyncAsAdmin_WithInValidId_NotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        int bookingId = 999;
        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId1,
            BatteryId = Seeder.BatteryId2,
            RentalDateTime = TestData.NowPlus15Days,
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var response = await _client.PutAsJsonAsync($"booking/{bookingId}", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBookingAsyncAsUser_WithValidId_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        int bookingId = Seeder.BookingId1;
        var model = new BookingDto.Mutate
        {
            BoatId = Seeder.BoatId1,
            BatteryId = Seeder.BatteryId2,
            RentalDateTime = TestData.NowPlus15Days,
            Status = BookingStatus.Active,
            UserId = Seeder.UserId,
            PriceId = Seeder.PriceId1,
            Remark = "testRemark",
        };

        // Act
        var response = await _client.PutAsJsonAsync($"booking/{bookingId}", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
