using System.IdentityModel.Tokens.Jwt;
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
using Rise.Shared.Addresses;
using Rise.Shared.Users;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class UserControllersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UserControllersTests(CustomWebApplicationFactory<Program> factory)
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
        //      "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjBVekktMHIzZk1KT3VGT2ctNU5YVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiVXNlciJdLCJpc3MiOiJodHRwczovL2J1dXQuZXUuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDY3MjExZmQzNzlhNzZjMjdjOTUxYzcwMSIsImF1ZCI6WyJodHRwczovL2FwaS5idXV0LmJlIiwiaHR0cHM6Ly9idXV0LmV1LmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE3MzE4ODU3NjIsImV4cCI6MTczMTk3MjE2Miwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSIsImF6cCI6InViOWdUOVprTDVsbVloVDJNQ1ZkVHFHS1pXRjR6ZXA5In0.bocqR3w0p0dctR5j-bl1GrpD6HEhoovf2_DubhbqkqdzwktP52gmleaqAK3McuEihnappTgCL65VUoJlQwQ7DVhbXdSjT9EdKX8c625Ho0Kj60FgCILQCH2oiucossZ6xb1kQKaiuWDLYYC-2OR1QbatVRkA2os4RScW5_cUmUMegqrV5E2MeVrp5SHMqSrTbRSoWfyKBwAYSFqREJTJ4h-CV6NVpjxACWrmz1d1BCmsvAUmc_co9u6el9oSdesasCcrWnmW3R3ljFLwtfgYqyPB2I2ebfsr6TCWwPRD9EA1XQ_q45afbvqEptL_6J3vNhS2Vyo8IdFAEaineKA_mQ";
        //  _adminToken =
        //      "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjBVekktMHIzZk1KT3VGT2ctNU5YVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiQWRtaW5pc3RyYXRvciJdLCJpc3MiOiJodHRwczovL2J1dXQuZXUuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDY3MjExZjY3NWNlZTMxNTNkZjU1OGRlZiIsImF1ZCI6WyJodHRwczovL2FwaS5idXV0LmJlIiwiaHR0cHM6Ly9idXV0LmV1LmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE3MzE4ODU2OTcsImV4cCI6MTczMTk3MjA5Nywic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSIsImF6cCI6InViOWdUOVprTDVsbVloVDJNQ1ZkVHFHS1pXRjR6ZXA5In0.g0nSHLKNbOwYQspvUcA_jLR1UKL4RGy1MMjQdky1hHXNmGBX38cYKimmoobZskBWbg0V5r9zDNdzFSUVbJaFyHYndUym4uxv_h-rNStZ5-D2cs1CaQ0XtX7NE9c_XRg98_4ubw328FFfreiGFnKCfDMWSxxBq39eQ4uCOnbU4_g_ac-N5g0OFNT4Bf0kKEjOheGRBDtf5m5Wz9EpXbSQKOuVeS4ElA2vU7NfHAXLsPtleTOD7Hg8hxdBVRoXSi8-yRwkwuVCxVfZcDNN8IIL0CTSwrvEy1Pfgr1G9jfzPhK7RIJ1LNuDvZf0ExeV6CrjwaOjc7QSJtXs2xsfYPLe_A";
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
    public async Task GetUserAsAdmin_WithKnownAuth0UserId_ReturnsOkAndUser()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromUser);
        UserDto.Index user = await response.Content.ReadFromJsonAsync<UserDto.Index>();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestData.Auth0UserIdFromUser, user.Auth0UserId);
        Assert.Equal(TestData.UserEmail, user.Email);
    }

    [Fact]
    public async Task GetUserAsUser_WithOwnAuth0UserId_ReturnsOkAndUser()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromUser);
        UserDto.Index user = await response.Content.ReadFromJsonAsync<UserDto.Index>();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestData.Auth0UserIdFromUser, user.Auth0UserId);
        Assert.Equal(TestData.UserEmail, user.Email);
    }

    [Fact]
    public async Task GetUserAsUser_WithSomeoneElsesAuth0UserId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromAdmin);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAsAdmin_WithUnKnownAuth0UserId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("users/" + TestData.UnknownAuth0UserId);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateNewUserAsAdmin_WithNewAuth0UserId_ReturnsOkAndUserId()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.PostAsJsonAsync(
            "register/" + TestData.UnknownAuth0UserId,
            TestData.AdminEmail
        );
        int userId = await response.Content.ReadFromJsonAsync<int>();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(userId > 0);
    }

    [Fact]
    public async Task CreateNewUserAsUser_WithOwnAuth0UserId_ReturnsOkAndUserId()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        ResetDatabaseWithoutSeed();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(_userToken);
        var auth0UserId = jwtToken.Claims.First(c => c.Type == "sub").Value;

        //Act
        var response = await _client.PostAsJsonAsync(
            "register/" + auth0UserId,
            TestData.AdminEmail
        );
        int userId = await response.Content.ReadFromJsonAsync<int>();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(userId > 0);
    }

    [Fact]
    public async Task CreateNewUserAsUser_WithNotOwnAuth0UserId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        ResetDatabaseWithoutSeed();
        //Act
        var response = await _client.PostAsJsonAsync(
            "register/" + "auth0|notown",
            TestData.AdminEmail
        );

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateNewUserAsAdmin_WithKnownAuth0UserId_ReturnsBadRequest()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.PostAsJsonAsync(
            "register/" + TestData.Auth0UserIdFromUser,
            TestData.UserEmail
        );

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "A user already exists with the specified auth0UserId.",
            await response.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task CompleteUserRegistrationAsAdmin_WithKnownAuth0UserId_ReturnsOk()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Arrange
        UserDto.Create userDto =
            new()
            {
                Auth0UserId = TestData.Auth0UserIdFromUser,
                Firstname = TestData.Firstname,
                Lastname = TestData.Lastname,
                PhoneNumber = TestData.PhoneNumber,
                BirthDay = TestData.BirthDay,
                Address = new()
                {
                    Street = TestData.Street,
                    HouseNumber = TestData.HouseNumber,
                    UnitNumber = TestData.UnitNumber,
                    PostalCode = TestData.PostalCode,
                    City = TestData.City,
                },
            };
        //Act
        var response = await _client.PostAsJsonAsync("register", userDto);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteUserRegistrationAsUser_WithOwnAuth0UserId_ReturnsOk()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Arrange
        UserDto.Create userDto =
            new()
            {
                Auth0UserId = TestData.Auth0UserIdFromUser,
                Firstname = TestData.Firstname,
                Lastname = TestData.Lastname,
                PhoneNumber = TestData.PhoneNumber,
                BirthDay = TestData.BirthDay,
                Address = new()
                {
                    Street = TestData.Street,
                    HouseNumber = TestData.HouseNumber,
                    UnitNumber = TestData.UnitNumber,
                    PostalCode = TestData.PostalCode,
                    City = TestData.City,
                },
            };
        //Act
        var response = await _client.PostAsJsonAsync("register", userDto);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteUserRegistrationAsUser_WithOthersAuth0UserId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Arrange
        UserDto.Create userDto =
            new()
            {
                Auth0UserId = TestData.Auth0UserIdFromAdmin,
                Firstname = TestData.Firstname,
                Lastname = TestData.Lastname,
                PhoneNumber = TestData.PhoneNumber,
                BirthDay = TestData.BirthDay,
                Address = new()
                {
                    Street = TestData.Street,
                    HouseNumber = TestData.HouseNumber,
                    UnitNumber = TestData.UnitNumber,
                    PostalCode = TestData.PostalCode,
                    City = TestData.City,
                },
            };
        //Act
        var response = await _client.PostAsJsonAsync("register", userDto);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompleteUserRegistrationAsAdmin_WithUnknownAuth0UserId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Arrange
        string unknownAuth0UserId = TestData.UnknownAuth0UserId;
        UserDto.Create userDto =
            new()
            {
                Auth0UserId = unknownAuth0UserId,
                Firstname = TestData.Firstname,
                Lastname = TestData.Lastname,
                PhoneNumber = TestData.PhoneNumber,
                BirthDay = TestData.BirthDay,
                Address = new()
                {
                    Street = TestData.Street,
                    HouseNumber = TestData.HouseNumber,
                    UnitNumber = TestData.UnitNumber,
                    PostalCode = TestData.PostalCode,
                    City = TestData.City,
                },
            };
        //Act
        var response = await _client.PostAsJsonAsync("register", userDto);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            $"User with auth0 user id {unknownAuth0UserId} not found.",
            await response.Content.ReadAsStringAsync()
        );
    }

    [Fact]
    public async Task CompleteUserRegistrationAsAdmin_WithKnownAuth0UserIdAndInvalidPhoneNumber_ReturnsInternalServerError()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        UserDto.Create userDto =
            new()
            {
                Auth0UserId = TestData.Auth0UserIdFromUser,
                Firstname = TestData.Firstname,
                Lastname = TestData.Lastname,
                PhoneNumber = TestData.InvalidPhoneNumber,
                BirthDay = TestData.BirthDay,
                Address = new()
                {
                    Street = TestData.Street,
                    HouseNumber = TestData.HouseNumber,
                    UnitNumber = TestData.UnitNumber,
                    PostalCode = TestData.PostalCode,
                    City = TestData.City,
                },
            };
        //Act
        var response = await _client.PostAsJsonAsync("register", userDto);

        //Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRegistrationStatusAsAdmin_WithKnownAuth0UserIdAndTrue_ReturnsOkAndUpdated()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromUser);
        UserDto.Index user = await response.Content.ReadFromJsonAsync<UserDto.Index>();

        //Act
        var response2 = await _client.PutAsJsonAsync(
            $"users/{TestData.Auth0UserIdFromUser}/registration-status",
            true
        );

        //Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var response3 = await _client.GetAsync("users/" + user.Auth0UserId);
        user = await response3.Content.ReadFromJsonAsync<UserDto.Index>();
        Assert.True(user.IsRegistrationComplete);
    }

    [Fact]
    public async Task UpdateUserRegistrationStatusAsUser_WithOwnAuth0UserIdAndTrue_ReturnsOkAndUpdated()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromUser);
        UserDto.Index user = await response.Content.ReadFromJsonAsync<UserDto.Index>();

        //Act
        var response2 = await _client.PutAsJsonAsync(
            $"users/{TestData.Auth0UserIdFromUser}/registration-status",
            true
        );

        //Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var response3 = await _client.GetAsync("users/" + user.Auth0UserId);
        user = await response3.Content.ReadFromJsonAsync<UserDto.Index>();
        Assert.True(user.IsRegistrationComplete);
    }

    [Fact]
    public async Task UpdateUserRegistrationStatusAsUser_WithSomeoneElsesAuth0UserIdAndTrue_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromAdmin);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRegistrationStatusAsAdmin_WithKnownAuth0UserIdAndFalse_ReturnsOkAndUpdated()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        var response = await _client.GetAsync("users/" + TestData.Auth0UserIdFromUser);
        UserDto.Index user = await response.Content.ReadFromJsonAsync<UserDto.Index>();

        //Act
        var response2 = await _client.PutAsJsonAsync(
            $"users/{TestData.Auth0UserIdFromUser}/registration-status",
            false
        );

        //Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var response3 = await _client.GetAsync("users/" + user.Auth0UserId);
        user = await response3.Content.ReadFromJsonAsync<UserDto.Index>();
        Assert.False(user.IsRegistrationComplete);
    }

    [Fact]
    public async Task UpdateUserRegistrationStatusAsAdmin_WithUnKnownAuth0UserId_ReturnsBadRequest()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        string unKnownAuth0UserId = "auth0|" + new Random().Next(10000, 999999999);

        //Act
        var response = await _client.PutAsJsonAsync(
            $"users/{unKnownAuth0UserId}/registration-status",
            true
        );

        //Assert
        Assert.Equal(
            $"User with id {unKnownAuth0UserId} not found.",
            await response.Content.ReadAsStringAsync()
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsersAsAdmin_ReturnsOkAndUsers()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("users");
        IEnumerable<UserDto.Index> users = await response.Content.ReadFromJsonAsync<
            IEnumerable<UserDto.Index>
        >();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(users.Count() > 0);
    }

    [Fact]
    public async Task GetAllUsersAsAdmin_emptyDatabase_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        ResetDatabaseWithoutSeed();

        //Act
        var response = await _client.GetAsync("users");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsersAsUser_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("users");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserIdAsyncAsAdmin_WithKnownAuth0UserId_ReturnsOkAndUserId()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("users/id/" + TestData.Auth0UserIdFromUser);
        int userId = await response.Content.ReadFromJsonAsync<int>();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(userId > 0);
    }

    [Fact]
    public async Task GetUserIdAsyncAsUser_WithOwnAuth0UserId_ReturnsOkAndUserId()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("users/id/" + TestData.Auth0UserIdFromUser);
        int userId = await response.Content.ReadFromJsonAsync<int>();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(userId > 0);
    }

    [Fact]
    public async Task GetUserIdAsyncAsUser_WithOtherAuth0UserId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("users/id/" + TestData.Auth0UserIdFromAdmin);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsAdmin_WithKnownUserId_ReturnsNoContent()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.DeleteAsync("users/" + Seeder.UserId);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsAdmin_WithUnKnownUserId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.DeleteAsync("users/" + 987785);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUserAsUser_WithOwnUserId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.DeleteAsync("users/" + Seeder.UserId);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserAsAdmin_WithKnownUserId_ReturnsOkAndUpdated()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        UserDto.Edit model = new UserDto.Edit
        {
            Id = Seeder.UserId,
            Firstname = "John",
            Lastname = "Travolta",
            PhoneNumber = "0477554477",
            BirthDay = new DateTime(2000, 1, 1),
            Address = new AddressDto
            {
                Street = "Kerkstraat",
                HouseNumber = "1",
                UnitNumber = "A",
                PostalCode = "9890",
                City = "Gavere",
            },
        };

        //Act
        var response = await _client.PutAsJsonAsync($"users/{Seeder.UserId}", model);

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedUser = db.Users.Find(Seeder.UserId);
            updatedUser.Firstname.ShouldBe("John");
            updatedUser.Lastname.ShouldBe("Travolta");
            updatedUser.PhoneNumber.ShouldBe("0477554477");
            updatedUser.BirthDay.ShouldBe(new DateTime(2000, 1, 1));
            var updatedUserAddress = db.Addresses.Find(updatedUser.AddressId);
            updatedUserAddress.Street.ShouldBe("Kerkstraat");
            updatedUserAddress.HouseNumber.ShouldBe("1");
            updatedUserAddress.UnitNumber.ShouldBe("A");
            updatedUserAddress.PostalCode.ShouldBe("9890");
            updatedUserAddress.City.ShouldBe("Gavere");
        }
    }

    [Fact]
    public async Task UpdateUserAsAdmin_WithUnKnownUserId_ReturnsNotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        UserDto.Edit model = new UserDto.Edit
        {
            Id = Seeder.UserId,
            Firstname = "John",
            Lastname = "Travolta",
            PhoneNumber = "0477554477",
            BirthDay = new DateTime(2000, 1, 1),
            Address = new AddressDto
            {
                Street = "Kerkstraat",
                HouseNumber = "1",
                UnitNumber = "A",
                PostalCode = "9890",
                City = "Gavere",
            },
        };

        //Act
        var response = await _client.PutAsJsonAsync($"users/987898", model);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserAsUser_WithOwnUserId_ReturnsOkAndUpdated()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        UserDto.Edit model = new UserDto.Edit
        {
            Id = Seeder.UserId,
            Firstname = "John",
            Lastname = "Travolta",
            PhoneNumber = "0477554477",
            BirthDay = new DateTime(2000, 1, 1),
            Address = new AddressDto
            {
                Street = "Kerkstraat",
                HouseNumber = "1",
                UnitNumber = "A",
                PostalCode = "9890",
                City = "Gavere",
            },
        };

        //Act
        var response = await _client.PutAsJsonAsync($"users/{Seeder.UserId}", model);

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedUser = db.Users.Find(Seeder.UserId);
            updatedUser.Firstname.ShouldBe("John");
            updatedUser.Lastname.ShouldBe("Travolta");
            updatedUser.PhoneNumber.ShouldBe("0477554477");
            updatedUser.BirthDay.ShouldBe(new DateTime(2000, 1, 1));
            var updatedUserAddress = db.Addresses.Find(updatedUser.AddressId);
            updatedUserAddress.Street.ShouldBe("Kerkstraat");
            updatedUserAddress.HouseNumber.ShouldBe("1");
            updatedUserAddress.UnitNumber.ShouldBe("A");
            updatedUserAddress.PostalCode.ShouldBe("9890");
            updatedUserAddress.City.ShouldBe("Gavere");
        }
    }

    [Fact]
    public async Task UpdateUserAsUser_WithOtherUserId_ReturnsForbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        UserDto.Edit model = new UserDto.Edit
        {
            Id = Seeder.UserId,
            Firstname = "John",
            Lastname = "Travolta",
            PhoneNumber = "0477554477",
            BirthDay = new DateTime(2000, 1, 1),
            Address = new AddressDto
            {
                Street = "Kerkstraat",
                HouseNumber = "1",
                UnitNumber = "A",
                PostalCode = "9890",
                City = "Gavere",
            },
        };

        //Act
        var response = await _client.PutAsJsonAsync($"users/{Seeder.AdminId}", model);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
