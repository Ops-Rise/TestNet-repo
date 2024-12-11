using Rise.Domain.Batteries;

namespace Rise.Shared.Batteries;

public interface IBatteryService
{
    Task<int> CreateBatteryAsync(BatteryDto.Create model);
    Task<IEnumerable<BatteryDto.BatteryIndex>?> GetAllBatteriesAsync();

    Task<IEnumerable<BatteryDto.BatteryDetail>?> GetAllBatteriesWithDetailsAsync();

    Task<IEnumerable<BatteryDto.BatteryIndex>?> GetBatteriesByStatusAsync(BatteryStatus status);

    Task<BatteryDto.BatteryIndex?> GetBatteryByIdAsync(int batteryId);

    Task<BatteryDto.BatteryDetail?> GetBatteryWithDetailsByIdAsync(int batteryId);

    Task<bool> UpdateBatteryAsync(int batteryId, BatteryDto.Mutate model);
    Task<bool> DeleteBatteryAsync(int batteryId);
}
