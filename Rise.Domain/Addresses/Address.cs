using System.Text.RegularExpressions;
using Rise.Domain.Batteries;

namespace Rise.Domain.Addresses
{
    public partial class Address : Entity
    {
        private string street = default!;
        public string Street
        {
            get => street;
            set => street = Guard.Against.NullOrWhiteSpace(value, nameof(Street));
        }

        private string houseNumber = default!;
        public string HouseNumber
        {
            get => houseNumber;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(HouseNumber));

                if (!HouseNumberRegex().IsMatch(value))
                {
                    throw new ArgumentException(
                        $"HouseNumber must start with number between 1 and 9. (Parameter '{nameof(HouseNumber)}')"
                    );
                }

                houseNumber = value;
            }
        }
        public string? UnitNumber { get; set; }
        private string city = default!;

        public string City
        {
            get => city;
            set => city = Guard.Against.NullOrWhiteSpace(value, nameof(City));
        }
        private string postalCode = default!;

        public string PostalCode
        {
            get => postalCode;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(PostalCode));

                if (!PostalCodeRegex().IsMatch(value))
                {
                    throw new ArgumentException(
                        $"PostalCode must be exactly 4 digits and cannot start with 0. (Parameter '{nameof(PostalCode)}')"
                    );
                }

                postalCode = value;
            }
        }

        public Address() { }

        public Address(
            string street,
            string houseNumber,
            string? unitNumber,
            string city,
            string postalCode
        )
        {
            Street = street;
            HouseNumber = houseNumber;
            UnitNumber = unitNumber;
            City = city;
            PostalCode = postalCode;
        }

        // Constructor without UnitNumber
        public Address(string street, string houseNumber, string city, string postalCode)
        {
            Street = street;
            HouseNumber = houseNumber;
            City = city;
            PostalCode = postalCode;
        }

        [GeneratedRegex(@"^(?!0)\d{4}$")]
        private static partial Regex PostalCodeRegex();

        [GeneratedRegex(@"^[1-9]\w*")]
        private static partial Regex HouseNumberRegex();
    }
}
