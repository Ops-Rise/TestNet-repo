using Microsoft.EntityFrameworkCore;
using Rise.Domain.Addresses;
using Rise.Domain.Users;
using Rise.Domain.ProfileImages;
using Rise.Persistence;
using Rise.Shared.Addresses;
using Rise.Shared.Users;
using Rise.Shared.ProfileImages;

namespace Rise.Services.Users;

public class UserService(ApplicationDbContext dbContext) : IUserService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IProfileImageService _profileImageService;

    public async Task<IEnumerable<UserDto.Index>?> GetAllAsync()
    {
        var users = await _dbContext
            .Users.Include(u => u.Address)
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto.Index
            {
                Id = u.Id,
                Auth0UserId = u.Auth0UserId,
                Email = u.Email,
                Firstname = u.Firstname ?? null,
                Lastname = u.Lastname ?? null,
                PhoneNumber = u.PhoneNumber ?? null,
                BirthDay = u.BirthDay ?? DateTime.MinValue,
                Address =
                    u.Address != null
                        ? new AddressDto
                        {
                            Id = u.Address.Id,
                            Street = u.Address.Street,
                            HouseNumber = u.Address.HouseNumber,
                            UnitNumber = u.Address.UnitNumber,
                            City = u.Address.City,
                            PostalCode = u.Address.PostalCode,
                        }
                        : null,
                IsTrainingComplete = u.IsTrainingComplete,
                IsRegistrationComplete = u.IsRegistrationComplete,
            })
            .OrderBy(u => u.IsTrainingComplete)
            .ToListAsync();

        return users;
    }

    public async Task<IEnumerable<UserDto.Index>?> GetVerifiedUsersAsync()
    {
        var verifiedUsers = await _dbContext
            .Users.Include(u => u.Address)
            .Where(u => !u.IsDeleted && u.IsTrainingComplete && u.IsRegistrationComplete)
            .Select(u => new UserDto.Index
            {
                Id = u.Id,
                Auth0UserId = u.Auth0UserId,
                Email = u.Email,
                Firstname = u.Firstname ?? null,
                Lastname = u.Lastname ?? null,
                PhoneNumber = u.PhoneNumber ?? null,
                BirthDay = u.BirthDay ?? DateTime.MinValue,
                Address =
                    u.Address != null
                        ? new AddressDto
                        {
                            Id = u.Address.Id,
                            Street = u.Address.Street,
                            HouseNumber = u.Address.HouseNumber,
                            UnitNumber = u.Address.UnitNumber,
                            City = u.Address.City,
                            PostalCode = u.Address.PostalCode,
                        }
                        : null,
                IsTrainingComplete = u.IsTrainingComplete,
                IsRegistrationComplete = u.IsRegistrationComplete,
            })
            .OrderBy(u => u.Lastname)
            .ToListAsync();

        return verifiedUsers;
    }

    public async Task<int> CreateUserWithMailAsync(string auth0UserId, string email)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Auth0UserId == auth0UserId))
            throw new InvalidOperationException(
                "A user already exists with the specified auth0UserId."
            );

        User user = new(auth0UserId, email);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user.Id;
    }

    public async Task<UserDto.Index?> GetUserAsync(string auth0UserId)
    {
        return await _dbContext
                .Users.Where(u => !u.IsDeleted && u.Auth0UserId == auth0UserId)
                .Select(u => new UserDto.Index
                {
                    Id = u.Id,
                    Auth0UserId = u.Auth0UserId,
                    Email = u.Email,
                    Firstname = u.Firstname ?? null,
                    Lastname = u.Lastname ?? null,
                    PhoneNumber = u.PhoneNumber ?? null,
                    BirthDay = u.BirthDay ?? DateTime.MinValue,
                    Address =
                        u.Address != null
                            ? new AddressDto
                            {
                                Id = u.Address.Id,
                                Street = u.Address.Street,
                                HouseNumber = u.Address.HouseNumber,
                                UnitNumber = u.Address.UnitNumber,
                                City = u.Address.City,
                                PostalCode = u.Address.PostalCode,
                            }
                            : null,
                    IsRegistrationComplete = u.IsRegistrationComplete,
                    IsTrainingComplete = u.IsTrainingComplete
                })
                .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"User with auth0 user id {auth0UserId} not found.");
    }

    public async Task CompleteUserRegistrationAsync(UserDto.Create userDto)
    {
        var user =
            await _dbContext.Users.FirstOrDefaultAsync(u => u.Auth0UserId == userDto.Auth0UserId)
            ?? throw new KeyNotFoundException(
                $"User with auth0 user id {userDto.Auth0UserId} not found."
            );

        user.Firstname =
            userDto.Firstname ?? throw new InvalidOperationException("Firstname is required.");
        user.Lastname =
            userDto.Lastname ?? throw new InvalidOperationException("Lastname is required.");
        user.PhoneNumber =
            userDto.PhoneNumber ?? throw new InvalidOperationException("PhoneNumber is required.");
        user.BirthDay =
            userDto.BirthDay ?? throw new InvalidOperationException("BirthDay is required.");
        if (userDto.Address is null)
        {
            throw new InvalidOperationException("Address is required.");
        }
        user.Address = new Address
        {
            Street =
                userDto.Address.Street
                ?? throw new InvalidOperationException("Street is required."),
            HouseNumber =
                userDto.Address.HouseNumber
                ?? throw new InvalidOperationException("HouseNumber is required."),
            UnitNumber = userDto.Address.UnitNumber,
            PostalCode =
                userDto.Address.PostalCode
                ?? throw new InvalidOperationException("PostalCode is required."),
            City = userDto.Address.City ?? throw new InvalidOperationException("City is required."),
        };
        user.IsRegistrationComplete = true;

        var profileImage = await _dbContext.ProfileImages
        .FirstOrDefaultAsync(p => p.UserId == user.Id && !p.IsDeleted);

        if (profileImage == null)
        {
            var defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "default_profile_image.png");

            var defaultImageBytes = await File.ReadAllBytesAsync(defaultImagePath);

            _dbContext.ProfileImages.Add(new ProfileImage(user.Id, defaultImageBytes, "image/png"));
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserRegistrationStatusAsync(string auth0UserId, bool isComplete)
    {
        var user =
            await _dbContext.Users.FirstOrDefaultAsync(u =>
                !u.IsDeleted && u.Auth0UserId == auth0UserId
            ) ?? throw new KeyNotFoundException($"User with id {auth0UserId} not found.");

        user.IsRegistrationComplete = isComplete;

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserTrainingStatusAsync(int userId, bool isTrainingComplete)
    {
        var user =
            await _dbContext.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == userId)
            ?? throw new InvalidOperationException($"User with id {userId} not found.");
        user.IsTrainingComplete = isTrainingComplete;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int userId)
    {
        var user =
            await _dbContext.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} was not found.");
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto.Index> UpdateAsync(int userId, UserDto.Edit model)
    {
        var user =
            await _dbContext
                .Users.Include(u => u.Address)
                .Where(u => !u.IsDeleted && u.Id == userId)
                .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"User with ID {userId} was not found.");

        user.Firstname =
            model.Firstname ?? throw new InvalidOperationException("Firstname is required.");
        user.Lastname =
            model.Lastname ?? throw new InvalidOperationException("Lastname is required.");
        user.PhoneNumber =
            model.PhoneNumber ?? throw new InvalidOperationException("PhoneNumber is required.");
        user.BirthDay =
            model.BirthDay ?? throw new InvalidOperationException("BirthDay is required.");
            
        var existingAddress = await _dbContext
            .Addresses.Where(a => !a.IsDeleted && a.Id == model.Address.Id)
            .FirstOrDefaultAsync();

        if (existingAddress is not null)
        {
            if (!string.Equals(existingAddress.Street, model.Address.Street, StringComparison.OrdinalIgnoreCase))
            {
                existingAddress.Street = model.Address.Street 
                    ?? throw new InvalidOperationException("Street is required.");
            }

            if (!string.Equals(existingAddress.HouseNumber, model.Address.HouseNumber, StringComparison.OrdinalIgnoreCase))
            {
                existingAddress.HouseNumber = model.Address.HouseNumber
                    ?? throw new InvalidOperationException("HouseNumber is required.");
            }

            if (!string.Equals(existingAddress.UnitNumber, model.Address.UnitNumber, StringComparison.OrdinalIgnoreCase))
            {
                existingAddress.UnitNumber = model.Address.UnitNumber;
            }

            if (!string.Equals(existingAddress.City, model.Address.City, StringComparison.OrdinalIgnoreCase))
            {
                existingAddress.City = model.Address.City 
                    ?? throw new InvalidOperationException("City is required.");
            }

            if (!string.Equals(existingAddress.PostalCode, model.Address.PostalCode, StringComparison.OrdinalIgnoreCase))
            {
                existingAddress.PostalCode = model.Address.PostalCode 
                    ?? throw new InvalidOperationException("PostalCode is required.");
            }

            user.AddressId = existingAddress.Id;
        }
        else
        {
            var newAddress = new Address
            {
                Street = model.Address.Street
                    ?? throw new InvalidOperationException("Street is required."),
                HouseNumber = model.Address.HouseNumber
                    ?? throw new InvalidOperationException("HouseNumber is required."),
                UnitNumber = model.Address.UnitNumber,
                City = model.Address.City ?? throw new InvalidOperationException("City is required."),
                PostalCode = model.Address.PostalCode
                    ?? throw new InvalidOperationException("PostalCode is required."),
            };

            _dbContext.Addresses.Add(newAddress);
            await _dbContext.SaveChangesAsync();

            user.AddressId = newAddress.Id;
        }

        await _dbContext.SaveChangesAsync();

        return new UserDto.Index
        {
            Id = user.Id,
            Email = user.Email,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            PhoneNumber = user.PhoneNumber,
            BirthDay = user.BirthDay,
            Auth0UserId = user.Auth0UserId,
            Address =
                user.Address != null
                    ? new AddressDto
                    {
                        Id = user.Address.Id,
                        Street = user.Address.Street,
                        HouseNumber = user.Address.HouseNumber,
                        UnitNumber = user.Address.UnitNumber,
                        City = user.Address.City,
                        PostalCode = user.Address.PostalCode,
                    }
                    : null,
            IsRegistrationComplete = user.IsRegistrationComplete,
        };
    }

    public Task<int> GetUserIdAsync(string auth0UserId)
    {
        Task<int> userId = _dbContext
            .Users.Where(u => !u.IsDeleted && u.Auth0UserId == auth0UserId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (userId.Result == 0)
        {
            throw new KeyNotFoundException($"User with auth0 user id {auth0UserId} not found.");
        }
        return userId;
    }

    public Task<string?> GetAuth0UserIdByUserId(int userId)
    {
        return _dbContext
                .Users.Where(u => !u.IsDeleted && u.Id == userId)
                .Select(u => u.Auth0UserId)
                .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"User with id {userId} not found.");
    }


}
