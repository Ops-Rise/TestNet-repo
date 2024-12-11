using Rise.Domain.Boats;

namespace Rise.Shared.Boats;

public abstract class BoatDto
{
    public class BoatIndex
    {
        public required int Id { get; set; }

        public required string Name { get; set; }

        public BoatStatus Status { get; set; }
    }

    public class Mutate
    {
        public required string Name { get; set; }
        public required BoatStatus Status { get; set; }
    }

    public class CreateBoatDto
    {
        public required string Name { get; set; }
        public required BoatStatus Status { get; set; }
    }


}
