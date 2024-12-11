using Rise.Domain.Boats;
using Rise.Fakers.Common;

namespace Rise.Fakers.BoatFakers;

public sealed class BoatFaker : EntityFaker<Boat>
{
    public BoatFaker(string locale = "nl") : base(locale)
    {
        CustomInstantiator(f => new Boat(
            $"Boot {f.PickRandom(new[] { "Limba", "Leith", "Lubek", "Lucia", "Lydian" })}",
            f.PickRandom<BoatStatus>()
        ));
    }
}
