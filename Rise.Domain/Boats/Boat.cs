using Rise.Domain.Bookings;

namespace Rise.Domain.Boats;

public class Boat : Entity
{
    private string name = default!;
    private BoatStatus status = default!;

    public string Name
    {
        get => name;
        set => name = Guard.Against.NullOrWhiteSpace(value, nameof(Name));
    }

    public BoatStatus Status
    {
        get => status;
        set => status = value;
    }

    private Boat() { }

    public ICollection<Booking> Bookings { get; } = new List<Booking>();

    public Boat(string name, BoatStatus status)
    {
        Name = name;
        Status = status;
    }
}
