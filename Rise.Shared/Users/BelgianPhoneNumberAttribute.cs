using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Rise.Shared.Validation
{
    public class BelgianPhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            string phoneNumber = value as string ?? string.Empty;

            if (Regex.IsMatch(phoneNumber, @"^04\d{8}$") || Regex.IsMatch(phoneNumber, @"^\+32\d{9}$"))
            {
                return ValidationResult.Success!;
            }

            return new ValidationResult(
                "Ongeldig Belgisch telefoonnummer. Het moet beginnen met 04 (10 cijfers) of +32 (11 cijfers)."
            );
        }
    }
}