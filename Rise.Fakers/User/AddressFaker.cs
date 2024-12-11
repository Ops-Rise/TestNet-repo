using Rise.Domain.Addresses;
using Rise.Fakers.Common;

namespace Rise.Fakers.User;

public sealed class AddressFaker : EntityFaker<Address>
{
    public AddressFaker(string locale = "nl") : base(locale)
    {
        CustomInstantiator(f => new Address(
            f.Address.StreetName(),
            f.Random.String2(1, 3, "123456789") + f.Random.String2(0, 2, "ABCDEF"),
            f.Random.Bool() ? f.Random.String2(1, 2, "0123456789") : null,
            f.Address.City(),
            ReplaceFirst(f.Random.Replace("####"), "0", "1")
        ));
    }
    private string ReplaceFirst(string text, string search, string replace)
    {
        int pos = text.IndexOf(search);
        if (pos < 0)
        {
            return text;
        }
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }
}
