using Microsoft.EntityFrameworkCore;
using Rise.Domain.ProfileImages;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Shared.ProfileImages;
using Rise.Shared.Users;

namespace Rise.Services.ProfileImages
{
    public class ProfileImageService : IProfileImageService
    {
        private readonly ApplicationDbContext _dbContext;

        public ProfileImageService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProfileImageDto.Detail?> GetProfileImageAsync(int userId)
        {
            var profileImage = await _dbContext.ProfileImages
                .Where(pi => pi.UserId == userId)
                .FirstOrDefaultAsync();

            if (profileImage == null || profileImage.ImageBlob.Length == 0)
            {
                return null;
            }

            return new ProfileImageDto.Detail
            {
                Id = profileImage.Id,
                ContentType = profileImage.ContentType ?? "image/png",
                ImageBlob = profileImage.ImageBlob,
                UserId = profileImage.UserId
            };
        }

        public async Task CreateProfileImageAsync(int userId, ProfileImageDto.Mutate profileImageDto)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .FirstOrDefaultAsync();
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} was not found.");
            }

            var profileImage = new ProfileImage(userId, profileImageDto.ImageBlob, profileImageDto.ContentType)
            {
                UserId = userId,
                ImageBlob = profileImageDto.ImageBlob,
                ContentType = profileImageDto.ContentType
            };

            await _dbContext.ProfileImages.AddAsync(profileImage);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateProfileImageAsync(int userId, ProfileImageDto.Edit profileImageDto)
        {
            var existingProfileImage = await _dbContext.ProfileImages
                .Where(pi => pi.UserId == userId && !pi.IsDeleted)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Profile image with userId {userId} was not found.");
            
            existingProfileImage.ImageBlob = profileImageDto.ImageBlob;
            existingProfileImage.ContentType = profileImageDto.ContentType;

            _dbContext.ProfileImages.Update(existingProfileImage);

            await _dbContext.SaveChangesAsync();
        }     

    }
}