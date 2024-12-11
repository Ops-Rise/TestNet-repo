using Rise.Fakers.Common;

namespace Rise.Fakers.User;

public sealed class UserFaker : EntityFaker<Rise.Domain.Users.User>
{
    public UserFaker(AddressFaker addressFaker, string locale = "nl") : base(locale)
    {
        CustomInstantiator(f => new Rise.Domain.Users.User(
            f.Random.Guid().ToString(),
            f.Internet.Email()
        )
        {
            Firstname = f.Name.FirstName(),
            Lastname = f.Name.LastName(),
            BirthDay = f.Date.Past(30, DateTime.Now.AddYears(-18)),
            PhoneNumber = f.Random.Bool() 
                ? f.Phone.PhoneNumber("04########")
                : f.Phone.PhoneNumber("+32#########"),
            Address = addressFaker.Generate(),
            IsRegistrationComplete = false,
            IsTrainingComplete = false
        });
    }
}
