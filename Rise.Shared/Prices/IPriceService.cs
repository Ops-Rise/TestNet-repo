namespace Rise.Shared.Prices
{
    public interface IPriceService
    {
        Task<IEnumerable<PriceDto.History>?> GetAllPricesAsync();
        Task<PriceDto.Index?> GetPriceAsync();
        Task<PriceDto.Index?> GetPriceByIdAsync(int priceId);
        Task<int> CreatePriceAsync(PriceDto.Create model);

    }
}
