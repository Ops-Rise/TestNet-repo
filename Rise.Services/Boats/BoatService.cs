
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Boats;

using Rise.Persistence;
using Rise.Shared.Boats;

namespace Rise.Services.Boats;

public class BoatService(ApplicationDbContext dbContext) : IBoatService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<IEnumerable<BoatDto.BoatIndex>?> GetAllBoatsAsync()
    {
        IQueryable<BoatDto.BoatIndex> query = _dbContext
            .Boats.Where(x => !x.IsDeleted)
            .Select(x => new BoatDto.BoatIndex { Id = x.Id, Name = x.Name, Status = x.Status });


        var boats = await query.ToListAsync();

        return boats;
    }

    public async Task<BoatDto.BoatIndex?> GetBoatByIdAsync(int boatId)
    {
        var boat =
            await _dbContext
                .Boats.Where(x => x.Id == boatId && !x.IsDeleted)
                .Select(x => new BoatDto.BoatIndex { Id = x.Id, Name = x.Name, Status = x.Status })
                .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Boat with ID {boatId} was not found.");
        return boat;
    }

    public async Task<BoatDto.BoatIndex> CreateNewBoatAsync(BoatDto.CreateBoatDto createDto)
    {   

        if (await _dbContext.Boats.AnyAsync(x => !x.IsDeleted &&  x.Name == createDto.Name))
        {
            throw new ArgumentException($"Boat with name {createDto.Name} already exists");
        }
        var newBoat = new Boat(createDto.Name, BoatStatus.Available);

        _dbContext.Boats.Add(newBoat);
        await _dbContext.SaveChangesAsync();

        return new BoatDto.BoatIndex { Id = newBoat.Id, Name = newBoat.Name };
    }

    public async Task<bool> UpdateBoatStatusAsync(
        int boatId,
        BoatDto.Mutate model
    )
    {
        var boat = await _dbContext.Boats.FindAsync(boatId);

        if (boat is null || boat.IsDeleted)
        {
            throw new KeyNotFoundException($"Boat with ID {boatId} was not found"); //boat not found or deleted
        }

        boat.Name = model.Name;
        boat.Status = model.Status;

        _dbContext.Boats.Update(boat);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteBoatAsync(int boatId)
    {
        var boat = await _dbContext.Boats.FindAsync(boatId);
        if (boat is null || boat.IsDeleted)
        {
            return false; //boat not found or deleted
        }

        boat.IsDeleted = true; //softDelete
        await _dbContext.SaveChangesAsync();
        return true;

    }

    public async Task<int> GetAvailableBoatsCountAsync()
    {
        var availableBoatsCount = await _dbContext
            .Boats.Where(x => !x.IsDeleted && x.Status == (int)BoatStatus.Available)
            .CountAsync();

        return availableBoatsCount;
    }
}
