using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rise.Domain.Addresses;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.Users;
using Rise.Shared.Addresses;
using Rise.Shared.Users;
using Shouldly;
using Xunit;

namespace Rise.Services.Tests
{
    public class UserServiceTests
    {
        private string auth0UserId = "auth0|123";
        private string email = "test@example.com";

        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Gebruik een unieke database per test
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateUserWithMailAsync_ShouldCreateUser_WhenAuth0UserIdDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);

            // Act
            int userId = await userService.CreateUserWithMailAsync(auth0UserId, email);

            // Assert
            var user = await dbContext.Users.FindAsync(userId);
            user.ShouldNotBeNull();
            user.Auth0UserId.ShouldBe(auth0UserId);
            user.Email.ShouldBe(email);
        }

        [Fact]
        public async Task CreateUserWithMailAsync_ShouldThrowInvalidOperationException_WhenAuth0UserIdExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);

            // Voeg al een gebruiker toe met dezelfde Auth0UserId
            dbContext.Users.Add(new User(auth0UserId, email));
            await dbContext.SaveChangesAsync();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(
                () => userService.CreateUserWithMailAsync(auth0UserId, "newemail@example.com")
            );
        }

        [Fact]
        public async Task GetUserAsync_ShouldReturnUser_WhenAuth0UserIdExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var user = new User(auth0UserId, email)
            {
                Firstname = "John",
                Lastname = "Doe",
                PhoneNumber = "0499887766",
                BirthDay = new DateTime(1990, 1, 1),
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userService.GetUserAsync(auth0UserId);

            // Assert
            result.ShouldNotBeNull();
            result.Firstname.ShouldBe("John");
            result.Lastname.ShouldBe("Doe");
        }

        [Fact]
        public async Task GetUserAsync_ShouldThrowKeyNotFoundException_WhenAuth0UserIdDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var user = new User(auth0UserId, email)
            {
                Firstname = "John",
                Lastname = "Doe",
                PhoneNumber = "0499887766",
                BirthDay = new DateTime(1990, 1, 1),
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act && Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(
                () => userService.GetUserAsync("auth0|wrong123")
            );
            //Assert
            Assert.Equal($"User with auth0 user id auth0|wrong123 not found.", exception.Message);
        }

        [Fact]
        public async Task CompleteUserRegistrationAsync_ShouldUpdateUser_WhenUserExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var existingUser = new User(auth0UserId, email);
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var userDto = new UserDto.Create
            {
                Auth0UserId = auth0UserId,
                Firstname = "John",
                Lastname = "Doe",
                PhoneNumber = "0477554477",
                BirthDay = new DateTime(1990, 1, 1),
                Address = new AddressDto
                {
                    Street = "Main St",
                    HouseNumber = "10",
                    PostalCode = "1234",
                    City = "Townsville",
                },
            };

            // Act
            await userService.CompleteUserRegistrationAsync(userDto);

            // Assert
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);
            user.ShouldNotBeNull();
            user.Firstname.ShouldBe("John");
            user.Lastname.ShouldBe("Doe");
            user.Address.ShouldNotBeNull();
            user.Address.City.ShouldBe("Townsville");
            user.IsRegistrationComplete.ShouldBe(true);
        }

        [Fact]
        public async Task CompleteUserRegistrationAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var existingUser = new User(auth0UserId, email);
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var userDto = new UserDto.Create
            {
                Auth0UserId = "auth0|wrong123",
                Firstname = "John",
                Lastname = "Doe",
                PhoneNumber = "0477554477",
                BirthDay = new DateTime(1990, 1, 1),
                Address = new AddressDto
                {
                    Street = "Main St",
                    HouseNumber = "10",
                    PostalCode = "1234",
                    City = "Townsville",
                },
            };

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(
                () => userService.CompleteUserRegistrationAsync(userDto)
            );
            //Assert
            Assert.Equal(
                $"User with auth0 user id {userDto.Auth0UserId} not found.",
                exception.Message
            );
        }

        [Fact]
        public async Task UpdateUserRegistrationStatusAsync_ShouldUpdateStatus_WhenUserExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var user = new User(auth0UserId, email) { IsRegistrationComplete = false };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act
            await userService.UpdateUserRegistrationStatusAsync(auth0UserId, true);

            // Assert
            var updatedUser = await dbContext.Users.FirstOrDefaultAsync(u =>
                u.Auth0UserId == auth0UserId
            );
            updatedUser.ShouldNotBeNull();
            updatedUser.IsRegistrationComplete.ShouldBe(true);
        }

        [Fact]
        public async Task UpdateUserRegistrationStatusAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);

            // Act & Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(
                () => userService.UpdateUserRegistrationStatusAsync("999", true)
            );

            // Assert
            Assert.Equal($"User with id 999 not found.", exception.Message);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsUsers()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var user1 = new User("auth0|123", "user1@example.com")
            {
                Firstname = "bart",
                Lastname = "smith",
                IsTrainingComplete = true,
                IsRegistrationComplete = true,
            };
            var user2 = new User("auth0|124", "user2@example.com")
            {
                Firstname = "jonas",
                Lastname = "defauw",
                IsTrainingComplete = false,
                IsRegistrationComplete = false,
            };

            dbContext.Users.AddRange(user1, user2);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userService.GetAllAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count().ShouldBe(2);

            var firstUser = result.FirstOrDefault(u => u.Auth0UserId == "auth0|123");
            firstUser.ShouldNotBeNull();
            firstUser.IsTrainingComplete.ShouldBe(true);

            var secondUser = result.FirstOrDefault(u => u.Auth0UserId == "auth0|124");
            secondUser.ShouldNotBeNull();
            secondUser.IsTrainingComplete.ShouldBe(false);
        }

        [Fact]
        public async Task GetAllAsync_emptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);

            // Act
            var users = await userService.GetAllAsync();

            //Assert
            users.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetUserIdAsync_ReturnsCorrectUserId()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user1 = new User(auth0UserId, email);
            User user2 = new User("auth0|456", "456@test.com");
            dbContext.Users.AddRangeAsync(user1, user2);
            await dbContext.SaveChangesAsync();

            // Act
            var userId = await userService.GetUserIdAsync(auth0UserId);

            //Assert
            userId.ShouldBe(user1.Id);
        }

        [Fact]
        public async Task GetUserIdAsync_NotExistingUser_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user1 = new User(auth0UserId, email);
            User user2 = new User("auth0|456", "456@test.com");
            dbContext.Users.AddRangeAsync(user1, user2);
            await dbContext.SaveChangesAsync();

            // Act && Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(
                () => userService.GetUserIdAsync("auth|wrong")
            );

            //Assert
            Assert.Equal($"User with auth0 user id auth|wrong not found.", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_DeletesUser()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            //Act
            var result = await userService.DeleteAsync(user.Id);

            //Assert
            result.ShouldBeTrue();
            var deletedUser = await dbContext.Users.FindAsync(user.Id);
            deletedUser.IsDeleted.ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateUserTrainingStatusAsync_ShouldUpdateTrainingStatus_WhenUserExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            var user = new User(auth0UserId, email) { IsTrainingComplete = false };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act
            await userService.UpdateUserTrainingStatusAsync(user.Id, true);

            // Assert
            var updatedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            updatedUser.ShouldNotBeNull();
            updatedUser.IsTrainingComplete.ShouldBe(true);
        }

        [Fact]
        public async Task UpdateUserTrainingStatusAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(
                () => userService.UpdateUserTrainingStatusAsync(999, true)
            );
        }

        [Fact]
        public async Task DeleteAsync_NotExistingUser_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act && Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(
                () => userService.DeleteAsync(999)
            );

            //Assert
            Assert.Equal($"User with ID 999 was not found.", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_ShouldMarkUserAsDeleted()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService =  new UserService(dbContext);
            var user = new User(auth0UserId, email);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act
            await userService.DeleteAsync(user.Id);

            // Assert
            var deletedUser = await dbContext.Users.FirstOrDefaultAsync(u =>
                u.Id == user.Id
            );
            deletedUser.ShouldNotBeNull();
            deletedUser.IsDeleted.ShouldBe(true);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUser()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            user.Firstname = "Jane";
            user.Lastname = "Smith";
            user.PhoneNumber = "0499887766";
            user.BirthDay = new DateTime(1990, 1, 1);
            user.Address = new Address
            {
                Street = "Parkstraat",
                HouseNumber = "10",
                PostalCode = "9000",
                City = "Gent",
            };
            user.IsRegistrationComplete = true;
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var model = new UserDto.Edit
            {
                Id = user.Id,
                Firstname = "John",
                Lastname = "Doe",
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

            // Act
            var updatedUser = await userService.UpdateAsync(user.Id, model);

            //Assert
            updatedUser.ShouldNotBeNull();
            updatedUser.Firstname.ShouldBe("John");
            updatedUser.Lastname.ShouldBe("Doe");
            updatedUser.PhoneNumber.ShouldBe("0477554477");
            updatedUser.BirthDay.ShouldBe(new DateTime(2000, 1, 1));
            updatedUser.Address.ShouldNotBeNull();
            updatedUser.Address.Street.ShouldBe("Kerkstraat");
            updatedUser.Address.HouseNumber.ShouldBe("1");
            updatedUser.Address.UnitNumber.ShouldBe("A");
            updatedUser.Address.PostalCode.ShouldBe("9890");
            updatedUser.Address.City.ShouldBe("Gavere");

            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Firstname == "John");
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Lastname == "Doe");
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.PhoneNumber == "0477554477");
            dbContext.Users.ShouldContain(x =>
                x.Id == user.Id && x.BirthDay == new DateTime(2000, 1, 1)
            );
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Address.Street == "Kerkstraat");
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Address.HouseNumber == "1");
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Address.UnitNumber == "A");
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Address.PostalCode == "9890");
            dbContext.Users.ShouldContain(x => x.Id == user.Id && x.Address.City == "Gavere");
        }

        [Fact]
        public async Task UpdateAsync_WrongUserId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            user.Firstname = "Jane";
            user.Lastname = "Smith";
            user.PhoneNumber = "0499887766";
            user.BirthDay = new DateTime(1990, 1, 1);
            user.Address = new Address
            {
                Street = "Parkstraat",
                HouseNumber = "10",
                PostalCode = "9000",
                City = "Gent",
            };
            user.IsRegistrationComplete = true;
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var model = new UserDto.Edit
            {
                Id = user.Id,
                Firstname = "John",
                Lastname = "Doe",
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

            // Act && Assert
            var exception = await Should.ThrowAsync<KeyNotFoundException>(
                () => userService.UpdateAsync(user.Id + 10, model)
            );

            //Assert
            Assert.Equal($"User with ID {user.Id + 10} was not found.", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_NoLastNameInModel_ThrowsInvalidOperationException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            user.Firstname = "Jane";
            user.Lastname = "Smith";
            user.PhoneNumber = "0499887766";
            user.BirthDay = new DateTime(1990, 1, 1);
            user.Address = new Address
            {
                Street = "Parkstraat",
                HouseNumber = "10",
                PostalCode = "9000",
                City = "Gent",
            };
            user.IsRegistrationComplete = true;
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var model = new UserDto.Edit
            {
                Id = user.Id,
                Firstname = "John",
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

            // Act && Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => userService.UpdateAsync(user.Id, model)
            );

            //Assert
            Assert.Equal($"Lastname is required.", exception.Message);
        }

        [Fact]
        public async Task GetAuth0UserIdByUserId_ReturnsAuth0UserId()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var userService = new UserService(dbContext);
            User user = new User(auth0UserId, email);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userService.GetAuth0UserIdByUserId(user.Id);

            //Assert
            result.ShouldBe(auth0UserId);
        }
    }
}
