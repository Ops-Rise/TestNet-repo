using System.Net;
using System.Net.Http.Json;
using Rise.Shared.Users;

namespace Rise.Client.Services
{
    public class UserService(HttpClient httpClient) : IUserService
    {
        private readonly HttpClient _httpClient = httpClient;
        private const string endpoint = "users";

        //om userId vast te houden in de applicatie vlak na inloggen
        private int _userId = -1;
        private bool _userIdFetched = false;

        public async Task<int> GetUserIdAsync(string auth0UserId)
        {
            if (_userIdFetched)
            {
                return _userId;
            }
            var response = await _httpClient.GetAsync($"{endpoint}/id/{auth0UserId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return -1;
            }
            response.EnsureSuccessStatusCode();

            _userId = int.Parse(await response.Content.ReadAsStringAsync());
            return _userId;
        }

        //aanroepen bij uitloggen!
        public void ResetUserId()
        {
            _userId = -1;
            _userIdFetched = false;
        }

        public async Task<IEnumerable<UserDto.Index>?> GetAllAsync()
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<UserDto.Index>>();
        }

        public async Task<IEnumerable<UserDto.Index>?> GetVerifiedUsersAsync()
        {
            var response = await _httpClient.GetAsync($"{endpoint}/verified");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IEnumerable<UserDto.Index>>();
        }

        public async Task<UserDto.Index> UpdateAsync(int userId, UserDto.Edit model)
        {
            var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{userId}", model);

            response.EnsureSuccessStatusCode();

            UserDto.Index user =
                await response.Content.ReadFromJsonAsync<UserDto.Index>()
                ?? throw new InvalidOperationException("Failed to deserialize the user.");
            return user;
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            var response = await _httpClient.DeleteAsync($"{endpoint}/{userId}");

            response.EnsureSuccessStatusCode();

            return response.IsSuccessStatusCode;
        }

        public async Task<int> CreateUserWithMailAsync(string auth0UserId, string email)
        {
            var response = await _httpClient.PostAsJsonAsync($"register/{auth0UserId}", email);

            response.EnsureSuccessStatusCode();

            var userId = await response.Content.ReadFromJsonAsync<int>();
            return userId;
        }

        public async Task CompleteUserRegistrationAsync(UserDto.Create userDto)
        {
            var response = await _httpClient.PostAsJsonAsync("register", userDto);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateUserRegistrationStatusAsync(string auth0UserId, bool isComplete)
        {
            var response = await _httpClient.PutAsync(
                $"{endpoint}/{auth0UserId}/registration-status",
                JsonContent.Create(isComplete)
            );
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateUserTrainingStatusAsync(int userId, bool isTrainingComplete)
        {
            var response = await httpClient.PutAsJsonAsync(
                $"{endpoint}/{userId}/activate",
                isTrainingComplete
            );
            response.EnsureSuccessStatusCode();
        }

        public async Task<UserDto.Index?> GetUserAsync(string auth0UserId)
        {
            var response = await _httpClient.GetAsync($"{endpoint}/{auth0UserId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<UserDto.Index>();
        }

        public Task<string?> GetAuth0UserIdByUserId(int userId)
        {
            //method only for backend service
            throw new NotImplementedException();
        }
    }
}
