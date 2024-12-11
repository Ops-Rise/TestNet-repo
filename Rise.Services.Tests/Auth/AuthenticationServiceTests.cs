/*using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Rise.Services.Auth; 
using Rise.Persistence.Users;
using Rise.Domain.Users;
using Rise.Shared.Auth;

namespace Rise.Services.Tests
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _authenticationService = new AuthenticationService(_mockUserRepository.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResult()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Name = "John Doe",
                Email = "johndoe@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
            };

            _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(user.Email))
                .ReturnsAsync(user);

            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "password123" 
            };

            // Act
            var result = await _authenticationService.LoginAsync(loginRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Email, result.User.Email);
            Assert.Equal("dummy-token", result.tokenDTO.AccessToken);
            Assert.Equal("dummy-token", result.tokenDTO.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "password123"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _authenticationService.LoginAsync(loginRequest));
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Name = "John Doe",
                Email = "johndoe@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
            };

            _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(user.Email))
                .ReturnsAsync(user);

            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "wrongpassword"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _authenticationService.LoginAsync(loginRequest));
        }

        [Fact]
        public async Task LoginAsync_EmptyEmail_ThrowsArgumentException()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = string.Empty,
                Password = "password123"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _authenticationService.LoginAsync(loginRequest));
            Assert.Equal("E-mail en wachtwoord zijn vereist.", ex.Message);
        }

        [Fact]
        public async Task LoginAsync_EmptyPassword_ThrowsArgumentException()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "johndoe@example.com",
                Password = string.Empty
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _authenticationService.LoginAsync(loginRequest));
            Assert.Equal("E-mail en wachtwoord zijn vereist.", ex.Message);
        }
    }
}
*/