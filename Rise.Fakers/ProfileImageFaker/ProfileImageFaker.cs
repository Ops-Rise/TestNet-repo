using Bogus;
using Rise.Fakers.Common;
using Rise.Domain.ProfileImages;

namespace Rise.Fakers.ProfileImageFakers;

public sealed class ProfileImageFaker : EntityFaker<ProfileImage>
{
    public ProfileImageFaker(IEnumerable<int> userIds)
    {
        CustomInstantiator(f => new ProfileImage(
            f.PickRandom(userIds),
            f.Random.Bytes(2048),
            f.PickRandom(new[] { "image/jpeg", "image/png", "image/gif" })
        ));
    }
}