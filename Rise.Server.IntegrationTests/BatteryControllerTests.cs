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
using Rise.Domain.Batteries;
using Rise.Persistence;
using Rise.Server.IntegrationTests.Utils;
using Rise.Shared.Batteries;
using Rise.Shared.Users;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class BatteryControllersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public BatteryControllersTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        ResetDatabaseWithSeed();

        if (_client == null)
        {
            factory.ClientOptions.BaseAddress = new Uri("https://localhost:5001/api/");
            _client = factory.CreateClient();
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
        //  _userToken =
        //  _adminToken =
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
    public async Task CreateBatteryAsAdmin_ValidBattery_ReturnsOkAndId()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        BatteryDto.Create newBattery = new BatteryDto.Create
        {
            Name = "Battery 99",
            UserId = Seeder.UserId,
        };

        //Act
        var response = await _client.PostAsJsonAsync("battery", newBattery);
        var responseBody = await response.Content.ReadAsStringAsync();
        int createdId = int.Parse(responseBody);

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(createdId > 0);
    }

    [Fact]
    public async Task CreateBatteryAsAdmin_InValidBatteryWrongUser_ReturnsBadRequest()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        BatteryDto.Create newBattery = new BatteryDto.Create
        {
            Name = "Battery 99",
            UserId = Seeder.UserId + 100,
        };

        //Act
        var response = await _client.PostAsJsonAsync("battery", newBattery);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("User does not exists.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateBatteryAsAdmin_InValidBatteryDuplicatename_ReturnsBadRequest()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        BatteryDto.Create newBattery = new BatteryDto.Create
        {
            Name = TestData.BatteryName1,
            UserId = Seeder.UserId,
        };

        //Act
        var response = await _client.PostAsJsonAsync("battery", newBattery);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            $"A battery with the name {TestData.BatteryName1} already exists.",
            await response.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task CreateBatteryAsUser_ValidBattery_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        BatteryDto.Create newBattery = new BatteryDto.Create
        {
            Name = "Battery 99",
            UserId = Seeder.UserId,
        };

        //Act
        var response = await _client.PostAsJsonAsync("battery", newBattery);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBatteriesAsAdmin_ReturnsAllBatteries()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("battery");
        var batteryList = await response.Content.ReadFromJsonAsync<
            IEnumerable<BatteryDto.BatteryIndex>
        >();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(batteryList);
        batteryList.Should().HaveCount(5);
        batteryList.Should().Contain(b => b.Name == TestData.BatteryName1);
        batteryList.Should().Contain(b => b.Name == TestData.BatteryName2);
        batteryList.Should().Contain(b => b.Name == TestData.BatteryName3);
        batteryList.Should().Contain(b => b.Name == TestData.BatteryName4);
        batteryList.Should().Contain(b => b.Name == TestData.BatteryName5);
    }

    [Fact]
    public async Task GetAllBatteriesAsAdmin_NoBatteries_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        ResetDatabaseWithoutSeed();

        //Act
        var response = await _client.GetAsync("battery");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBatteriesAsUser_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("battery");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBatteriesWithDetailsAsAdmin_ReturnsAllBatteries()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("battery/details");
        var batteryList = await response.Content.ReadFromJsonAsync<
            IEnumerable<BatteryDto.BatteryDetail>
        >();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(batteryList);
        batteryList.Should().HaveCount(5);
        BatteryDto.BatteryDetail batteryDto1 = batteryList.FirstOrDefault(x =>
            x.Name == TestData.BatteryName1
        );
        batteryDto1.UseCycles.ShouldBe(2);
        batteryDto1.DateLastUsed.ShouldBe(TestData.NowPlus6Days);
        BatteryDto.BatteryDetail batteryDto2 = batteryList.FirstOrDefault(x =>
            x.Name == TestData.BatteryName2
        );
        batteryDto2.UseCycles.ShouldBe(4);
        batteryDto2.DateLastUsed.ShouldBe(TestData.NowPlus15Days);
    }

    [Fact]
    public async Task GetAllBatteriesWithDetailsAsAdmin_NoBatteries_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        ResetDatabaseWithoutSeed();

        //Act
        var response = await _client.GetAsync("battery/details");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllBatteriesWithDetailsAsUser_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("battery/details");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBatteriesByStatusAsAdmin_ReturnsAllCorrectBatteries()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response1 = await _client.GetAsync("battery/status/Available");
        var availableList = await response1.Content.ReadFromJsonAsync<
            IEnumerable<BatteryDto.BatteryIndex>
        >();
        var response2 = await _client.GetAsync("battery/status/Reserve");
        var reserveList = await response2.Content.ReadFromJsonAsync<
            IEnumerable<BatteryDto.BatteryIndex>
        >();
        var response3 = await _client.GetAsync("battery/status/OutOfService");
        var outOfServiceList = await response3.Content.ReadFromJsonAsync<
            IEnumerable<BatteryDto.BatteryIndex>
        >();
        var response4 = await _client.GetAsync("battery/status/InRepair");
        var inRepairList = await response4.Content.ReadFromJsonAsync<
            IEnumerable<BatteryDto.BatteryIndex>
        >();

        //Assert
        response1.EnsureSuccessStatusCode();
        Assert.NotNull(response1);
        Assert.NotEmpty(availableList);
        availableList.Should().HaveCount(2);
        availableList.Should().Contain(b => b.Name == TestData.BatteryName1);
        availableList.Should().Contain(b => b.Name == TestData.BatteryName2);
        reserveList.Should().HaveCount(1);
        reserveList.Should().Contain(b => b.Name == TestData.BatteryName3);
        outOfServiceList.Should().HaveCount(1);
        outOfServiceList.Should().Contain(b => b.Name == TestData.BatteryName4);
        inRepairList.Should().HaveCount(1);
        inRepairList.Should().Contain(b => b.Name == TestData.BatteryName5);
    }

    [Fact]
    public async Task GetBatteriesByStatusAsAdmin_NoBatteries_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        ResetDatabaseWithoutSeed();

        //Act
        var response = await _client.GetAsync("battery/status/Available");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBatteriesAsUser_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("battery/status/Available");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBatteryByIdAsAdmin_ValidId_ReturnBattery()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync($"battery/{Seeder.BatteryId1}");
        var battery = await response.Content.ReadFromJsonAsync<BatteryDto.BatteryIndex>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotNull(battery);
        battery.Name.ShouldBe(TestData.BatteryName1);
    }

    [Fact]
    public async Task GetBatteryByIdAsAdmin_InValidId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync($"battery/{12547865}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "Battery with id 12547865 not found.",
            await response.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task GetBatteryByIdAsUser_ValidId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync($"battery/12547865");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBatteryWithDetailsByIdAsAdmin_ValidId_ReturnBattery()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync($"battery/details/{Seeder.BatteryId1}");
        var battery = await response.Content.ReadFromJsonAsync<BatteryDto.BatteryDetail>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotNull(battery);
        battery.Name.ShouldBe(TestData.BatteryName1);
        battery.UseCycles.ShouldBe(2);
        battery.DateLastUsed.ShouldBe(TestData.NowPlus6Days);
    }

    [Fact]
    public async Task GetBatteryWithDetailsByIdAsAdmin_InValidId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync($"battery/details/12547865");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "Battery with id 12547865 not found.",
            await response.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task GetBatteryWithDetailsByIdAsUser_ValidId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync($"battery/details/12547865");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBatteryAsAdmin_ValidModelCorrectId_ReturnsOk()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        BatteryDto.Mutate mutateModel = new BatteryDto.Mutate
        {
            Name = "Battery 99",
            Status = BatteryStatus.OutOfService,
            UserId = Seeder.AdminId,
        };

        //Act
        var response = await _client.PutAsJsonAsync($"battery/{Seeder.BatteryId1}", mutateModel);

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedBattery = db.Batteries.Find(Seeder.BatteryId1);
            updatedBattery.Name.ShouldBe(mutateModel.Name);
            updatedBattery.Status.ShouldBe(BatteryStatus.OutOfService);
            updatedBattery.UserId.ShouldBe(Seeder.AdminId);
        }
    }

    [Fact]
    public async Task UpdateBatteryAsAdmin_ValidModelInCorrectId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        BatteryDto.Mutate mutateModel = new BatteryDto.Mutate
        {
            Name = "Battery 99",
            Status = BatteryStatus.OutOfService,
            UserId = Seeder.AdminId,
        };

        //Act
        var response = await _client.PutAsJsonAsync($"battery/{12345}", mutateModel);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBatteryAsUser_ValidModel_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        BatteryDto.Mutate mutateModel = new BatteryDto.Mutate
        {
            Name = "Battery 99",
            Status = BatteryStatus.OutOfService,
            UserId = Seeder.AdminId,
        };

        //Act
        var response = await _client.PutAsJsonAsync($"battery/{Seeder.BatteryId1}", mutateModel);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBatteryAsAdmin_ValidId_ReturnsNoContent()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.DeleteAsync($"battery/{Seeder.BatteryId1}");

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deletedBattery = db.Batteries.Find(Seeder.BatteryId1);
            Assert.True(deletedBattery.IsDeleted);
        }
    }

    [Fact]
    public async Task DeleteBatteryAsAdmin_InValidId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.DeleteAsync($"battery/12345");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBatteryAsUser_ValidId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.DeleteAsync($"battery/{Seeder.BatteryId1}");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
