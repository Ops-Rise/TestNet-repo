using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rise.Domain.Boats;
using Rise.Persistence;
using Rise.Services.Boats;
using Rise.Shared.Boats;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Services.Tests
{
    public class BoatServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly BoatService _boatService;
        private readonly ITestOutputHelper _output;

        public BoatServiceTests(ITestOutputHelper output)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "BoatTestDb")
                .Options;

            _output = output;
            _dbContext = new ApplicationDbContext(options);
            _boatService = new BoatService(_dbContext);
        }

        private ApplicationDbContext CreateNewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Fact]
        public async Task GetAllBoatsAsync_ReturnsAllNonDeletedBoats()
        {
            // Arrange
            var boat1 = new Boat("Boat 1", BoatStatus.Available);
            var boat2 = new Boat("Boat 2", BoatStatus.Available);
            var boat3 = new Boat("Boat 3", BoatStatus.InRepair);
            var boat4 = new Boat("Boat 4", BoatStatus.OutOfService);
            var deletedBoat = new Boat("Boat 5", BoatStatus.Available) { IsDeleted = true };

            _dbContext.Boats.AddRange(boat1, boat2, boat3, boat4, deletedBoat);
            await _dbContext.SaveChangesAsync();

            // Act
            var boats = await _boatService.GetAllBoatsAsync();

            // Assert
            Assert.Equal(4, boats.Count());
            Assert.Contains(boats, b => b.Name == "Boat 1");
            Assert.Contains(boats, b => b.Name == "Boat 2");
            Assert.Contains(boats, b => b.Name == "Boat 3");
            Assert.Contains(boats, b => b.Name == "Boat 4");
            Assert.DoesNotContain(boats, b => b.Name == "Boat 5");
        }

        [Fact]
        public async Task CreateNewBoatAsync_ValidBoat_ReturnsCreatedBoat()
        {
            // Arrange
            var createDto = new BoatDto.CreateBoatDto
            {
                Name = "New Boat",
                Status = BoatStatus.Available,
            };

            // Act
            var createdBoat = await _boatService.CreateNewBoatAsync(createDto);

            // Assert
            Assert.NotNull(createdBoat);
            Assert.Equal(createDto.Name, createdBoat.Name);

            var boatInDb = await _dbContext.Boats.FindAsync(createdBoat.Id);
            Assert.NotNull(boatInDb);
            Assert.Equal(createDto.Name, boatInDb.Name);
            Assert.Equal(BoatStatus.Available, boatInDb.Status);
        }

        [Fact]
        public async Task GetBoatByIdAsync_ExistingBoatId_ReturnsBoatWithStatus()
        {
            // Arrange
            var boat = new Boat("Existing Boat", BoatStatus.Available);
            _dbContext.Boats.Add(boat);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _boatService.GetBoatByIdAsync(boat.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(boat.Id, result.Id);
            Assert.Equal(boat.Name, result.Name);

            var boatInDb = await _dbContext.Boats.FindAsync(boat.Id);
            Assert.Equal(BoatStatus.Available, boatInDb.Status);
        }

        [Fact]
        public async Task GetBoatByIdAsync_NonExistingBoatId_ThrowsKeyNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _boatService.GetBoatByIdAsync(999)
            );
        }

        [Fact]
        public async Task UpdateBoatStatusAsync_ValidId_UpdatesStatus()
        {
            // Arrange
            var boat = new Boat("Test Boat", BoatStatus.Available);
            _dbContext.Boats.Add(boat);
            await _dbContext.SaveChangesAsync();

            var updateModel = new BoatDto.Mutate
            {
                Name = "Test Boat",
                Status = BoatStatus.InRepair,
            };

            // Act
            var updatedBoatIndex = await _boatService.UpdateBoatStatusAsync(boat.Id, updateModel);

            // Assert
            Assert.NotNull(updatedBoatIndex);
            Assert.Equal(BoatStatus.InRepair, boat.Status);

            var updatedBoat = await _dbContext.Boats.FindAsync(boat.Id);
            Assert.Equal(BoatStatus.InRepair, updatedBoat.Status);
        }

        [Fact]
        public async Task UpdateBoatStatusAsync_InvalidId_ThrowsKeyNotFoundException()
        {
            var updateModel = new BoatDto.Mutate
            {
                Name = "Test Boat",
                Status = BoatStatus.InRepair,
            };
            // Act && Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _boatService.UpdateBoatStatusAsync(999, updateModel)
            );
        }

        [Fact]
        public async Task DeleteBoatAsync_ValidId_SoftDeletesBoat()
        {
            // Arrange
            var boat = new Boat("Test Boat", BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _boatService.DeleteBoatAsync(boat.Id);
            var deletedBoat = await _dbContext.Boats.FindAsync(boat.Id);

            // Assert
            Assert.True(result);
            Assert.True(deletedBoat.IsDeleted);
        }

        [Fact]
        public async Task DeleteBoatAsync_InvalidId_ReturnsFalse()
        {
            // Act
            var result = await _boatService.DeleteBoatAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAvalaibleBoatsCountAsync_ReturnsCountOfAvailableBoats()
        {
            // Arrange
            var availableBoat1 = new Boat("Available Boat 1", BoatStatus.Available);
            var availableBoat2 = new Boat("Available Boat 2", BoatStatus.Available);
            var inRepairBoat1 = new Boat("In repair Boat", BoatStatus.InRepair);
            var outOfServiceBoat = new Boat("Out of Service Boat", BoatStatus.OutOfService);
            var inRepairBoat2 = new Boat("In Repair Boat", BoatStatus.InRepair);
            var deletedBoat = new Boat("Deleted Boat", BoatStatus.Available) { IsDeleted = true };

            _dbContext.Boats.AddRange(
                availableBoat1,
                availableBoat2,
                inRepairBoat1,
                outOfServiceBoat,
                inRepairBoat2,
                deletedBoat
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var count = await _boatService.GetAvailableBoatsCountAsync();

            // Assert
            Assert.Equal(2, count);
        }
    }
}
