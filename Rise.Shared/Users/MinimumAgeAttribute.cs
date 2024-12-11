using System.ComponentModel.DataAnnotations;

namespace Rise.Shared.Validation
{
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            ErrorMessage = $"Leeftijd moet minstens {_minimumAge} jaar zijn.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime birthDate)
            {
                var today = DateTime.Today;
                var age = today.Year - birthDate.Year;
                if (birthDate.Date > today.AddYears(-age)) age--;

                if (age < _minimumAge) return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}