using System.ComponentModel.DataAnnotations;
using Rise.Shared.Validation;

namespace Rise.Client.Pages
{
    public class RegisterAccountForm
    {
        [Required(ErrorMessage = "Voornaam is verplicht.")]
        [StringLength(50, ErrorMessage = "Voornaam mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Voornaam moet minstens {1} karakters bevatten.")]
        public string Firstname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Achternaam is verplicht.")]
        [StringLength(50, ErrorMessage = "Achternaam mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Achternaam moet minstens {1} karakters bevatten.")]
        public string Lastname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Straatnaam is verplicht.")]
        [StringLength(100, ErrorMessage = "Straat mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Straat moet minstens {1} karakters bevatten.")]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "Huisnummer is verplicht.")]
        [StringLength(6, ErrorMessage = "Huisnummer mag max {1} karakters bevatten.")]
        [RegularExpression(@"^[1-9]\w*", ErrorMessage = "Huisnummer moet beginnen met een cijfer van 1 tot 9.")]
        public string HouseNumber { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Bus mag max {1} karakters bevatten.")]
        public string? UnitNumber { get; set; }

        [Required(ErrorMessage = "Postcode is verplicht.")]
        [RegularExpression(@"^[1-9]\d{3}$", ErrorMessage = "Postcode moet exact 4 cijfers bevatten, niet starten met 0.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Plaats is verplicht.")]
        [StringLength(50, ErrorMessage = "Plaats mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Plaats moet minstens {1} karakters bevatten.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefoonnummer is verplicht.")]
        [BelgianPhoneNumber]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Geboortedatum is verplicht.")]
        [MinimumAge(18)]
        public DateTime? BirthDay { get; set; }
    }
}