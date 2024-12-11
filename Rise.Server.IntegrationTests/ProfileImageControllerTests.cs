using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Rise.Server.IntegrationTests.Utils;
using Rise.Shared.ProfileImages;
using Rise.Shared.Users;
using Rise.Persistence;
using FluentAssertions;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class ProfileImageControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ProfileImageControllerTests(CustomWebApplicationFactory<Program> factory)
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
        if (role == Role.ADMIN && !string.IsNullOrEmpty(_adminToken))
        {
            return _adminToken;
        }
        else if (role == Role.USER && !string.IsNullOrEmpty(_userToken))
        {
            return _userToken;
        }

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
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            var seeder = new Seeder(db);
            seeder.Seed();
        }
    }

    [Fact]
    public async Task GetProfileImageAsync_AsAdmin_ReturnsProfileImage()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetAsync($"profileimage/{Seeder.UserProfileImageId}");
        var profileImage = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(profileImage);
        profileImage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProfileImageAsync_AsUser_ReturnsProfileImage()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync($"profileimage/{Seeder.UserProfileImageId}");
        var profileImage = await response.Content.ReadAsByteArrayAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(profileImage);
        profileImage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProfileImageAsync_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);

        // Act
        var response = await _client.GetAsync($"profileimage/{Seeder.AdminProfileImageId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProfileImageAsync_ProfileImageNotFound_ReturnsNotFound()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        // Act
        var response = await _client.GetAsync("profileimage/999"); // Non-existing profile image

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProfileImageAsync_AsAdmin_CreatesProfileImage()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var profileImageDto = new ProfileImageDto.Mutate
        {
            ContentType = "image/png",
            ImageBlob = new byte[] { 1, 2, 3, 4 },
            UserId = Seeder.UserId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"profileimage/{Seeder.TestUserId}", profileImageDto);

        var profileImageResponse = await _client.GetAsync($"profileimage/{Seeder.TestUserId}");

        profileImageResponse.EnsureSuccessStatusCode();

        var contentType = profileImageResponse.Content.Headers.ContentType?.MediaType ?? "image/png";
        var imageBlob = await profileImageResponse.Content.ReadAsByteArrayAsync();

        // Assert
        Assert.Equal(profileImageDto.ContentType, contentType);
        Assert.Equal(profileImageDto.ImageBlob, imageBlob);
    }

    [Fact]
    public async Task CreateProfileImageAsync_AsUser_ReturnsForbidden()
    {
        // Arrange
        await SetAuthorizationHeader(Role.USER);
        var profileImageDto = new ProfileImageDto.Mutate
        {
            ContentType = "image/png",
            ImageBlob = new byte[] { 1, 2, 3, 4 },
            UserId = Seeder.UserId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"profileimage/{Seeder.TestUserId}", profileImageDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProfileImageAsync_InvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var profileImageDto = new ProfileImageDto.Mutate
        {
            ContentType = "",
            ImageBlob = new byte[] { 1, 2, 3, 4 },
            UserId = Seeder.UserId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"profileimage/{Seeder.TestUserId}", profileImageDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProfileImageAsync_NoImageBlob_ReturnsBadRequest()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var profileImageDto = new ProfileImageDto.Mutate
        {
            ContentType = "image/png",
            ImageBlob = null,
            UserId = Seeder.UserId
        };

        // Act
        var response = await _client.PostAsJsonAsync($"profileimage/{Seeder.TestUserId}", profileImageDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProfileImageAsync_ServerError_ReturnsInternalServerError()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var profileImageDto = new ProfileImageDto.Mutate
        {
            ContentType = "image/png",
            ImageBlob = new byte[] { 1, 2, 3, 4 },
            UserId = 9999 // Non-existing user ID for error
        };

        // Act
        var response = await _client.PostAsJsonAsync($"profileimage/{profileImageDto.UserId}", profileImageDto);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task EditProfileImageAsync_AsAdmin_UpdatesProfileImage()
    {
        // Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var profileImageDto = new ProfileImageDto.Edit
        {
            Id = 1,
            ContentType = "image/jpeg",
            ImageBlob = new byte[] { 5, 6, 7, 8 }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"profileimage/{Seeder.UserProfileImageId}", profileImageDto);
        response.EnsureSuccessStatusCode();

        var updatedImageResponse = await _client.GetAsync($"profileimage/{Seeder.UserProfileImageId}");

        updatedImageResponse.EnsureSuccessStatusCode();

        var contentType = updatedImageResponse.Content.Headers.ContentType?.MediaType ?? "image/png";
        var imageBlob = await updatedImageResponse.Content.ReadAsByteArrayAsync();

        // Assert
        Assert.Equal(profileImageDto.ContentType, contentType);
        Assert.Equal(profileImageDto.ImageBlob, imageBlob);
    }
}