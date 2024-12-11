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
using Rise.Persistence;
using Rise.Server.IntegrationTests.Utils;
using Rise.Shared.TimeSlots;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class TimeSlotControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TimeSlotControllerTests(CustomWebApplicationFactory<Program> factory)
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

    private void ResetDatabaseWithSeed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            var seeder = new Seeder(db);
            seeder.Seed();
        }
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

    [Fact]
    public async Task GetTimeSlots_WithValidDateRange_AsAdmin_ReturnsOk()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var startDate = DateTime.Today.AddDays(1);
        var endDate = startDate.AddDays(30);

        // Act
        var response = await _client.GetAsync(
            $"timeslot?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
        );
        var timeSlots = await response.Content.ReadFromJsonAsync<IEnumerable<TimeSlotDto>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        timeSlots.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSlots_WithInvalidDateRange_AsAdmin_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var startDate = DateTime.Today.AddDays(30);
        var endDate = startDate.AddDays(-1); // End date before start date

        // Act
        var response = await _client.GetAsync(
            $"timeslot?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTimeSlots_WithPastStartDate_AsAdmin_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var startDate = DateTime.Today.AddDays(-1);
        var endDate = DateTime.Today.AddDays(30);

        // Act
        var response = await _client.GetAsync(
            $"timeslot?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTimeSlots_WithRangeTooLarge_AsAdmin_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var startDate = DateTime.Today;
        var endDate = startDate.AddDays(366); // More than 365 days

        // Act
        var response = await _client.GetAsync(
            $"timeslot?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BlockTimeSlot_WithValidModel_AsAdmin_ReturnsOk()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var model = new TimeSlotDto
        {
            Date = DateTime.Today.AddDays(5),
            TimeSlot =
                0 // Morning slot
            ,
        };

        // Act
        var response = await _client.PostAsJsonAsync("timeslot/block", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BlockTimeSlot_WithPastDate_AsAdmin_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var model = new TimeSlotDto { Date = DateTime.Today.AddDays(-1), TimeSlot = 0 };

        // Act
        var response = await _client.PostAsJsonAsync("timeslot/block", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BlockTimeSlot_AsUser_ReturnsForbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        var model = new TimeSlotDto { Date = DateTime.Today.AddDays(5), TimeSlot = 0 };

        // Act
        var response = await _client.PostAsJsonAsync("timeslot/block", model);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnblockTimeSlot_WithValidParams_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var date = DateTime.Today.AddDays(5);
        var timeSlot = 0;

        // First block the timeslot
        var blockModel = new TimeSlotDto { Date = date, TimeSlot = timeSlot };
        await _client.PostAsJsonAsync("timeslot/block", blockModel);

        // Act
        var response = await _client.DeleteAsync(
            $"timeslot/unblock?date={date:yyyy-MM-dd}&timeSlot={timeSlot}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnblockTimeSlot_WithPastDate_AsAdmin_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var date = DateTime.Today.AddDays(-1);
        var timeSlot = 0;

        // Act
        var response = await _client.DeleteAsync(
            $"timeslot/unblock?date={date:yyyy-MM-dd}&timeSlot={timeSlot}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnblockTimeSlot_AsUser_ReturnsForbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        var date = DateTime.Today.AddDays(5);
        var timeSlot = 0;

        // Act
        var response = await _client.DeleteAsync(
            $"timeslot/unblock?date={date:yyyy-MM-dd}&timeSlot={timeSlot}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
