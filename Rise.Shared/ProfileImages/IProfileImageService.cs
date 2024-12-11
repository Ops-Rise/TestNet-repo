using System.Threading;

namespace Rise.Shared.ProfileImages;

public interface IProfileImageService
{
    Task CreateProfileImageAsync(int userId, ProfileImageDto.Mutate profileImageDto);
    Task UpdateProfileImageAsync(int userId, ProfileImageDto.Edit profileImageDto);
    Task<ProfileImageDto.Detail?> GetProfileImageAsync(int userId);
}