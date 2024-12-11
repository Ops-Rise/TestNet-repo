namespace Rise.Shared.Users;

public interface IUserService
{
    Task<IEnumerable<UserDto.Index>?> GetAllAsync();
    Task<int> GetUserIdAsync(string auth0UserId);
    Task<IEnumerable<UserDto.Index>?> GetVerifiedUsersAsync();
    Task<int> CreateUserWithMailAsync(string auth0UserId, string email);
    Task<UserDto.Index?> GetUserAsync(string auth0UserId);
    Task<UserDto.Index> UpdateAsync(int userId, UserDto.Edit model);
    Task CompleteUserRegistrationAsync(UserDto.Create userDto);
    Task UpdateUserRegistrationStatusAsync(string auth0UserId, bool isComplete);
    Task UpdateUserTrainingStatusAsync(int userId, bool isTrainingComplete);
    Task<bool> DeleteAsync(int userId);
    Task<string?> GetAuth0UserIdByUserId (int userId);
}

