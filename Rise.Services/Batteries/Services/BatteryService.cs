using Microsoft.EntityFrameworkCore;
using Rise.Domain.Batteries;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Shared.Addresses;
using Rise.Shared.Batteries;
using Rise.Domain.Batteries;
using Rise.Shared.Users;

namespace Rise.Services.Batteries.Services
{
    public class BatteryService(ApplicationDbContext dbContext) : IBatteryService
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<int> CreateBatteryAsync(BatteryDto.Create model)
        {
            if (await _dbContext.Batteries.AnyAsync(x => !x.IsDeleted && x.Name == model.Name))
                throw new ArgumentException(
                    $"A battery with the name {model.Name} already exists."
                );

            var user =
                await _dbContext.Users.FirstOrDefaultAsync(x =>
                    !x.IsDeleted && x.Id == model.UserId
                ) ?? throw new ArgumentException("User does not exists.");

            Battery battery = new(model.Name, BatteryStatus.Available, user);
            _dbContext.Batteries.Add(battery);
            await _dbContext.SaveChangesAsync();

            return battery.Id;
        }

        public async Task<IEnumerable<BatteryDto.BatteryIndex>?> GetAllBatteriesAsync()
        {
            IQueryable<BatteryDto.BatteryIndex> query = _dbContext
                .Batteries.Where(b => !b.IsDeleted)
                .Select(x => new BatteryDto.BatteryIndex
                {
                    Id = x.Id,
                    Name = x.Name,
                    Status = x.Status,
                });

            var batteries = await query.ToListAsync();

            return batteries;
        }

        public async Task<IEnumerable<BatteryDto.BatteryDetail>?> GetAllBatteriesWithDetailsAsync()
        {
            var batteries = await _dbContext
                .Batteries.Where(b => !b.IsDeleted)
                .Select(b => new BatteryDto.BatteryDetail
                {
                    Id = b.Id,
                    Name = b.Name,
                    Status = b.Status,
                    User = new UserDto.Index
                    {
                        Id = b.User.Id,
                        Firstname = b.User.Firstname ?? null,
                        Lastname = b.User.Lastname ?? null,
                        PhoneNumber = b.User.PhoneNumber ?? null,
                        Address = b.User.Address != null
                            ? new AddressDto
                            {
                                Id = b.User.Address.Id,
                                Street = b.User.Address.Street,
                                HouseNumber = b.User.Address.HouseNumber,
                                UnitNumber = b.User.Address.UnitNumber,
                                City = b.User.Address.City,
                                PostalCode = b.User.Address.PostalCode,
                            }
                            : null,
                    },
                    DateLastUsed = b
                        .Bookings.Where(booking =>
                            !booking.IsDeleted && booking.Status != BookingStatus.Canceled
                        )
                        .OrderByDescending(booking => booking.RentalDateTime)
                        .Select(booking => booking.RentalDateTime)
                        .FirstOrDefault(),

                    UseCycles = b.Bookings.Count(booking =>
                        !booking.IsDeleted && booking.Status != BookingStatus.Canceled
                    ),
                })
                .ToListAsync();

            return batteries;
        }

        public async Task<IEnumerable<BatteryDto.BatteryIndex>?> GetBatteriesByStatusAsync(
            BatteryStatus status
        )
        {
            // Convert the Enum to the BatteryStatus enum
            var batteryStatus = status;

            IQueryable<BatteryDto.BatteryIndex> query = _dbContext
                .Batteries.Where(x => x.Status == batteryStatus && !x.IsDeleted)
                .Select(x => new BatteryDto.BatteryIndex
                {
                    Id = x.Id,
                    Name = x.Name,
                    Status = x.Status,
                });

            var batteries = await query.ToListAsync();

            return batteries;
        }

        public async Task<BatteryDto.BatteryIndex?> GetBatteryByIdAsync(int batteryId)
        {
            var battery =
                await _dbContext
                    .Batteries.Where(x => x.Id == batteryId && !x.IsDeleted)
                    .Select(x => new BatteryDto.BatteryIndex
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Status = x.Status,
                    })
                    .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Battery with id {batteryId} not found.");
            return battery;
        }

        public async Task<bool> UpdateBatteryAsync(int batteryId, BatteryDto.Mutate model)
        {
            var battery =
                await _dbContext.Batteries.FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == batteryId)
                ?? throw new KeyNotFoundException($"Battery with id {batteryId} not found.");

            battery.Status = model.Status;
            battery.Name = model.Name;
            battery.UserId = model.UserId;
            _dbContext.Batteries.Update(battery);

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<BatteryDto.BatteryDetail?> GetBatteryWithDetailsByIdAsync(int batteryId)
        {
            var batteryDetail =
                await _dbContext
                    .Batteries.Where(b => b.Id == batteryId && !b.IsDeleted)
                    .Select(b => new BatteryDto.BatteryDetail
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Status = b.Status,
                        User = new UserDto.Index
                        {
                            Id = b.User.Id,
                            Auth0UserId = b.User.Auth0UserId,
                            Email = b.User.Email,
                            Firstname = b.User.Firstname ?? null,
                            Lastname = b.User.Lastname ?? null,
                            PhoneNumber = b.User.PhoneNumber ?? null,
                            BirthDay = b.User.BirthDay ?? DateTime.MinValue,
                            Address =
                                b.User.Address != null
                                    ? new AddressDto
                                    {
                                        Id = b.User.Address.Id,
                                        Street = b.User.Address.Street,
                                        HouseNumber = b.User.Address.HouseNumber,
                                        UnitNumber = b.User.Address.UnitNumber,
                                        City = b.User.Address.City,
                                        PostalCode = b.User.Address.PostalCode,
                                    }
                                    : null,
                            IsRegistrationComplete = b.User.IsRegistrationComplete,
                        },

                        DateLastUsed = b
                            .Bookings.Where(booking =>
                                !booking.IsDeleted
                                && booking.Status != BookingStatus.Canceled
                            )
                            .OrderByDescending(booking => booking.RentalDateTime)
                            .Select(booking => booking.RentalDateTime)
                            .FirstOrDefault(),

                        UseCycles = b.Bookings.Count(booking =>
                            !booking.IsDeleted && booking.Status != BookingStatus.Canceled
                        ),
                    })
                    .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Battery with id {batteryId} not found.");
            return batteryDetail;
        }

        public async Task<bool> DeleteBatteryAsync(int batteryId)
        {
            var battery =
                await _dbContext.Batteries.FindAsync(batteryId)
                ?? throw new KeyNotFoundException($"Battery with id {batteryId} not found.");
            battery.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}
