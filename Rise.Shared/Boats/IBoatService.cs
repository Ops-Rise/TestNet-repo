using System.Threading;

namespace Rise.Shared.Boats;

public interface IBoatService
{

    Task<IEnumerable<BoatDto.BoatIndex>?> GetAllBoatsAsync();
    Task<BoatDto.BoatIndex> CreateNewBoatAsync(BoatDto.CreateBoatDto createDto);
    Task<int> GetAvailableBoatsCountAsync();
    Task<BoatDto.BoatIndex?> GetBoatByIdAsync(int boatId);
    Task<bool> UpdateBoatStatusAsync(int boatId, BoatDto.Mutate model);
    Task<bool> DeleteBoatAsync(int boatId);
}
