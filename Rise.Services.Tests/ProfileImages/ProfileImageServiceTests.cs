using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.ProfileImages;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.ProfileImages;
using Rise.Shared.ProfileImages;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Services.Tests
{
    public class ProfileImageServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ProfileImageService _profileImageService;
        private readonly ITestOutputHelper _output;

        private string auth0UserId = "auth0|123";
        private string email = "test@example.com";

        public ProfileImageServiceTests(ITestOutputHelper output)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ProfileImageTestDb")
                .Options;

            _output = output;
            _dbContext = new ApplicationDbContext(options);
            _profileImageService = new ProfileImageService(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Fact]
        public async Task CreateProfileImageAsync_ValidInput_CreatesProfileImage()
        {
            // Arrange
            var userId = 1;
            var profileImageDto = new ProfileImageDto.Mutate
            {
                UserId = userId,
                ContentType = "image/png",
                ImageBlob = new byte[] { 1, 2, 3 }
            };

            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            await _profileImageService.CreateProfileImageAsync(userId, profileImageDto);

            // Assert
            var profileImage = await _dbContext.ProfileImages
                .Where(pi => pi.UserId == userId)
                .FirstOrDefaultAsync();
            
            Assert.NotNull(profileImage);
            Assert.Equal(userId, profileImage.UserId);
            Assert.Equal(profileImageDto.ContentType, profileImage.ContentType);
            Assert.Equal(profileImageDto.ImageBlob, profileImage.ImageBlob);
        }

        [Fact]
        public async Task GetProfileImageAsync_ExistingUser_ReturnsProfileImage()
        {
            // Arrange
            var userId = 1;
            var profileImage = new ProfileImage(userId, new byte[] { 1, 2, 3 }, "image/jpeg");
            _dbContext.ProfileImages.Add(profileImage);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _profileImageService.GetProfileImageAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(profileImage.ContentType, result.ContentType);
            Assert.Equal(profileImage.ImageBlob, result.ImageBlob);
        }

        [Fact]
        public async Task GetProfileImageAsync_UserNotFound_ReturnsNull()
        {
            // Act
            var result = await _profileImageService.GetProfileImageAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ValidInput_UpdatesProfileImage()
        {
            // Arrange
            var userId = 1;
            var existingImage = new ProfileImage(userId, new byte[] { 1, 2, 3 }, "image/jpeg");
            _dbContext.ProfileImages.Add(existingImage);
            await _dbContext.SaveChangesAsync();

            var editDto = new ProfileImageDto.Edit
            {
                Id = existingImage.Id,
                ContentType = "image/png",
                ImageBlob = new byte[] { 4, 5, 6 }
            };

            // Act
            await _profileImageService.UpdateProfileImageAsync(userId, editDto);

            // Assert
            var updatedProfileImage = await _dbContext.ProfileImages.FindAsync(existingImage.Id);
            Assert.NotNull(updatedProfileImage);
            Assert.Equal(editDto.ContentType, updatedProfileImage.ContentType);
            Assert.Equal(editDto.ImageBlob, updatedProfileImage.ImageBlob);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ImageNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var editDto = new ProfileImageDto.Edit
            {
                Id = 999,
                ContentType = "image/png",
                ImageBlob = new byte[] { 4, 5, 6 }
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _profileImageService.UpdateProfileImageAsync(1, editDto)
            );
        }

        [Fact]
        public async Task CreateProfileImageAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = 999;
            var profileImageDto = new ProfileImageDto.Mutate
            {
                UserId = userId,
                ContentType = "image/png",
                ImageBlob = new byte[] { 1, 2, 3 }
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _profileImageService.CreateProfileImageAsync(userId, profileImageDto)
            );
        }
    }
}