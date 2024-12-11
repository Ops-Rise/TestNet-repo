using Rise.Domain.Batteries;
using Rise.Shared.Users;

namespace Rise.Shared.Batteries;

public abstract class BatteryDto
{
    public class BatteryIndex
    {
        public required int Id { get; set; }

        public required string Name { get; set; }

        public required BatteryStatus Status { get; set; }
    }

    public class Create
    {
        public required string Name { get; set; }
        public required int UserId { get; set; }
    }

    public class Mutate
    {
        public required string Name { get; set; }
        public required BatteryStatus Status { get; set; }
        public required int UserId { get; set; }
    }

    public class BatteryDetail
    {
        public required int Id { get; set; }

        public required string Name { get; set; }

        public required BatteryStatus Status { get; set; }

        public UserDto.Index User { get; set; }

        public  DateTime DateLastUsed { get; set; }

        public  int UseCycles { get; set; }
    }
}
