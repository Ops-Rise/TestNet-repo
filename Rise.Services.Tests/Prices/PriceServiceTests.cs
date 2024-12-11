using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Prices;
using Rise.Persistence;
using Rise.Services.Prices;
using Rise.Shared.Prices;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Services.Tests
{
    public class PriceServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PriceService _PriceService;
        private readonly ITestOutputHelper _output;

        public PriceServiceTests(ITestOutputHelper output)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "PriceTestDb")
                .Options;

            _output = output;
            _dbContext = new ApplicationDbContext(options);
            _PriceService = new PriceService(_dbContext);
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
        public async Task GetAllPricesAsync_ReturnsAllNonDeletedPrices()
        {
            var price1 = new Price((decimal)20.99);
            var price2 = new Price((decimal)30);
            var price3 = new Price((decimal)45);
            var price4 = new Price((decimal)49.99);
            var deletedPrice = new Price((decimal)24.99) { IsDeleted = true };

            _dbContext.Prices.AddRange(price1, price2, price3, price4, deletedPrice);
            await _dbContext.SaveChangesAsync();

            // Act
            var prices = await _PriceService.GetAllPricesAsync();

            // Assert
            Assert.NotNull(prices);
            Assert.Equal(4, prices.Count());
            Assert.DoesNotContain(prices, p => p.Id == deletedPrice.Id);
        }

        [Fact]
        public async  Task GetPriceAsync_ReturnsLatestPrice()
        {
            // Arrange
            List<Price> prices =
                new()
                {
                    new Price((decimal)20.99) { CreatedAt = DateTime.UtcNow.AddSeconds(-4) },
                    new Price((decimal)30) { CreatedAt = DateTime.UtcNow.AddSeconds(-3) },
                    new Price((decimal)40) { CreatedAt = DateTime.UtcNow.AddSeconds(0) },
                    new Price((decimal)49.99) { CreatedAt = DateTime.UtcNow.AddSeconds(-1) },
                };
    
                _dbContext.Prices.AddRange(prices);
                await _dbContext.SaveChangesAsync();
        
            // Act
            var price = await _PriceService.GetPriceAsync();

            // Assert
            Assert.NotNull(price);
            Assert.Equal((decimal)40, price.Amount);
        }

        [Fact]
        public async Task GetPriceByIdAsync_ReturnsPriceById()
        {
            var price1 = new Price((decimal)20.99);
            var price2 = new Price((decimal)30);
            var price3 = new Price((decimal)45);
            var price4 = new Price((decimal)49.99);

            _dbContext.Prices.AddRange(price1, price2, price3, price4);
            await _dbContext.SaveChangesAsync();

            // Act
            var price = await _PriceService.GetPriceByIdAsync(price3.Id);

            // Assert
            Assert.NotNull(price);
            Assert.Equal((decimal)45, price.Amount);
        }

        [Fact]
        public async Task GetPriceById_InvalidId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invalidId = 999;

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _PriceService.GetPriceByIdAsync(invalidId)
            );
        }

        [Fact]
        public async Task CreatePriceAsync_CreatesNewPrice()
        {
            // Arrange
            var createDto = new PriceDto.Create { Amount = (decimal)20.99 };

            // Act
            var newPriceId = await _PriceService.CreatePriceAsync(createDto);

            // Assert
            var newPrice = await _dbContext.Prices.FindAsync(newPriceId);
            Assert.NotNull(newPrice);
            Assert.Equal(createDto.Amount, newPrice.Amount);
        }
    }
}
