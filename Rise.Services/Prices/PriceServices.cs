using Microsoft.EntityFrameworkCore;
using Rise.Domain.Prices;
using Rise.Persistence;
using Rise.Shared.Prices;

namespace Rise.Services.Prices;

public class PriceService(ApplicationDbContext dbContext) : IPriceService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<IEnumerable<PriceDto.History>?> GetAllPricesAsync()
    {
        IQueryable<PriceDto.History> query = _dbContext
            .Prices.Where(p => !p.IsDeleted)
            .Select(p => new PriceDto.History
            {
                Id = p.Id,
                Amount = p.Amount,
                CreatedAt = p.CreatedAt,
            })
            .OrderByDescending(p => p.CreatedAt);

        var prices = await query.ToListAsync();
        return prices;
    }

    public async Task<PriceDto.Index?> GetPriceAsync()
    {
        return await _dbContext
            .Prices.Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(1)
            .Select(p => new PriceDto.Index { Id = p.Id, Amount = p.Amount })
            .SingleOrDefaultAsync();
    }

    public async Task<PriceDto.Index?> GetPriceByIdAsync(int priceId)
    {
        var price =
            await _dbContext
                .Prices.Where(p => !p.IsDeleted && p.Id == priceId)
                .Select(p => new PriceDto.Index { Id = p.Id, Amount = p.Amount })
                .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Price with ID {priceId} was not found.");
        return price;
    }

    public async Task<int> CreatePriceAsync(PriceDto.Create model)
    {
        Price price = new(model.Amount);
        _dbContext.Prices.Add(price);
        await _dbContext.SaveChangesAsync();
        return price.Id;
    }
}
