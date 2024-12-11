using Rise.Domain.Batteries;
using Rise.Fakers.User;
using Rise.Fakers.Common;

namespace Rise.Fakers.BatteryFakers;

public sealed class BatteryFaker : EntityFaker<Battery>
{
    public BatteryFaker(UserFaker userFaker, string locale = "nl") : base(locale)
    {
        CustomInstantiator(f =>
        {
            var user = userFaker.Generate();    
            return new Battery(
                $"Battery-B{f.Random.Number(20, 99)}",
                f.PickRandom<BatteryStatus>(),
                user
            );
        });
    }
}
