using System.Net;
using System.Net.Http.Json;
using Rise.Shared.ProfileImages;

namespace Rise.Client.Services
{
    public class ProfileImageService : IProfileImageService
    {
        private readonly HttpClient _httpClient;
        private const string endpoint = "profileimage";

        public ProfileImageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProfileImageDto.Detail?> GetProfileImageAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{endpoint}/{userId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to fetch profile image for user {userId}. Status code: {response.StatusCode}");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var imageBlob = await response.Content.ReadAsByteArrayAsync();
            if (imageBlob == null || imageBlob.Length == 0)
            {
                throw new InvalidOperationException($"No image data returned for user {userId}.");
            }

            return new ProfileImageDto.Detail
            {
                Id = userId,
                ContentType = contentType,
                ImageBlob = imageBlob
            };
        }

        public async Task UpdateProfileImageAsync(int userId, ProfileImageDto.Edit imageDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{userId}", imageDto);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to update profile image.");
            }
        }

        public async Task CreateProfileImageAsync(int userId, ProfileImageDto.Mutate mutateDto)
        {
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}/{userId}", mutateDto);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to create profile image.");
            }
        }
    }
}