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
using Rise.Domain.Prices;
using Rise.Persistence;
using Rise.Server.IntegrationTests.Utils;
using Rise.Shared.Prices;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class PriceControllersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public PriceControllersTests(CustomWebApplicationFactory<Program> factory)
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
        //   _userToken =
        // _adminToken =
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
    public async Task GetAllPricesAsyncAsAdmin_ReturnsAllNonDeletedPrices()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetFromJsonAsync<IEnumerable<PriceDto.History>>("price");

        // Assert
        response.Should().NotBeNull();
        response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllPricesAsyncAsAdmin_emptyDatabase_NotFound()
    {
        // Arrange
        ResetDatabaseWithoutSeed();
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetAsync("price");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllPricesAsyncAsUser_ReturnsForbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync("price");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPriceAsyncAsAdmin_ReturnsLatestPrice()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetFromJsonAsync<PriceDto.Index>("price/latest");

        // Assert
        response.Should().NotBeNull();
        response.Amount.Should().Be(30m);
    }

    [Fact]
    public async Task GetPriceAsyncAsUser_ReturnsLatestPrice()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetFromJsonAsync<PriceDto.Index>("price/latest");

        // Assert
        response.Should().NotBeNull();
        response.Amount.Should().Be(30m);
    }

    [Fact]
    public async Task GetPriceByIdAsyncAsAdmin_ReturnsPriceById()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetFromJsonAsync<PriceDto.Index>($"price/{Seeder.PriceId1}");

        // Assert
        response.Should().NotBeNull();
        response.Amount.Should().Be(20.99m);
    }

    [Fact]
    public async Task GetPriceByIdAsyncAsUser_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync($"price/{Seeder.PriceId1}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPriceByIdAsyncAsAdmin_NonExistingPriceId_ReturnsNotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetAsync("price/999");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePriceAsyncAsAdmin_CreatesNewPrice()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var newPrice = new PriceDto.Create { Amount = 25.99m };

        // Act
        var response = await _client.PostAsJsonAsync("price", newPrice);
        var responseBody = await response.Content.ReadAsStringAsync();
        int createdId = int.Parse(responseBody);

        // Assert
        response.EnsureSuccessStatusCode();

        response.Should().NotBeNull();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var price = db.Prices.Find(createdId);
            price.Amount.ShouldBe(newPrice.Amount);
        }
    }

    [Fact]
    public async Task CreatePriceAsyncAsUser_Forbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        var newPrice = new PriceDto.Create { Amount = 25.99m };

        // Act
        var response = await _client.PostAsJsonAsync("price", newPrice);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
