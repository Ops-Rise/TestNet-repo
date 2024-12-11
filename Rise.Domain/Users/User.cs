using System.Text.RegularExpressions;
using Rise.Domain.Addresses;
using Rise.Domain.Batteries;
using Rise.Domain.Bookings;
using Rise.Domain.TimeSlots;

namespace Rise.Domain.Users;

public partial class User : Entity
{
    private string auth0UserId = default!;
    public string Auth0UserId
    {
        get => auth0UserId;
        set => auth0UserId = Guard.Against.NullOrWhiteSpace(value, nameof(Auth0UserId));
    }
    private string email = default!;
    public string Email
    {
        get => email;
        set => email = Guard.Against.NullOrWhiteSpace(value, nameof(Email));
    }
    private string firstname = default!;
    public string Firstname
    {
        get => firstname;
        set => firstname = Guard.Against.NullOrWhiteSpace(value, nameof(Firstname));
    }
    private string lastname = default!;
    public string Lastname
    {
        get => lastname;
        set => lastname = Guard.Against.NullOrWhiteSpace(value, nameof(Lastname));
    }
    private DateTime? birthDay = default!;
    public DateTime? BirthDay
    {
        get => birthDay;
        set
        {
            if (value.HasValue)
            {
                DateTime today = DateTime.Today;
                int age = today.Year - value.Value.Year;
                if (value.Value.Date > today.AddYears(-age))
                    age--;
                if (age < 18)
                {
                    throw new ArgumentException("BirthDay must be at least 18 years ago.");
                }
            }
            birthDay = value;
        }
    }
    private string phoneNumber = default!;
    public string PhoneNumber
    {
        get => phoneNumber;
        set
        {
            if (!(PhoneNumber1Regex().IsMatch(value) || PhoneNumber2Regex().IsMatch(value)))
            {
                throw new ArgumentException(
                    $"PhoneNumber must be Belgian format (+32 or 04) (Parameter '{nameof(PhoneNumber)}')"
                );
            }

            phoneNumber = value;
        }
    }

    public int? AddressId { get; set; }

    private Address? address = default!;
    public Address? Address
    {
        get => address;
        set => address = Guard.Against.Null(value, nameof(Address));
    }

    private bool isRegistrationComplete = default!;
    public bool IsRegistrationComplete
    {
        get => isRegistrationComplete;
        set => isRegistrationComplete = value;
    }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Battery> Batteries { get; set; } = new List<Battery>();

    public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

    public User(string auth0UserId, string email)
    {
        Auth0UserId = auth0UserId;
        Email = email;
    }

    private bool isTrainingComplete = false;
    public bool IsTrainingComplete
    {
        get => isTrainingComplete;
        set => isTrainingComplete = value;
    }

    //password and roles on auth0 database

    [GeneratedRegex(@"^04\d{8}$")]
    private static partial Regex PhoneNumber1Regex();

    [GeneratedRegex(@"^\+32\d{9}$")]
    private static partial Regex PhoneNumber2Regex();
}
