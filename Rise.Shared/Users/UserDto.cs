using System.Text.Json.Serialization;
using FluentValidation;
using Rise.Shared.Addresses;

namespace Rise.Shared.Users
{
    public abstract class UserDto
    {
        public class Index
        {
            public  int Id { get; set; }
            public string? Email { get; set; }
            [JsonPropertyName("firstName")]
            public string? Firstname { get; set; }
            [JsonPropertyName("lastName")]
            public string? Lastname { get; set; }
            public string? PhoneNumber { get; set; }
            public DateTime? BirthDay { get; set; }
            public string? Auth0UserId { get; set; }
            public AddressDto? Address { get; set; }
            public bool IsRegistrationComplete { get; set; }
            public bool IsTrainingComplete { get; set; }
            public string FullName => $"{Firstname} {Lastname}";

        }

        public abstract class BaseDto
        {
            public string? Firstname { get; set; }
            public string? Lastname { get; set; }
            public string? PhoneNumber { get; set; }
            public DateTime? BirthDay { get; set; }
            public AddressDto? Address { get; set; }
            public bool IsRegistrationComplete { get; set; }

            protected static void ApplyBaseRules<T>(AbstractValidator<T> validator)
                where T : BaseDto
            {
                validator
                    .RuleFor(x => x.Firstname)
                    .NotEmpty()
                    .MinimumLength(2)
                    .MaximumLength(50)
                    .WithMessage("Firstname is required.");
                validator
                    .RuleFor(x => x.Lastname)
                    .NotEmpty()
                    .MinimumLength(2)
                    .MaximumLength(50)
                    .WithMessage("Lastname is required.");
                validator
                    .RuleFor(x => x.PhoneNumber)
                    .NotEmpty()
                    .MinimumLength(10)
                    .MaximumLength(12)
                    .WithMessage("PhoneNumber is required.");
                validator.RuleFor(x => x.BirthDay).NotEmpty().WithMessage("BirthDay is required.");
                validator
                    .RuleFor(x => x.Address)
                    .NotNull()
                    .WithMessage("Address is required.")
                    .DependentRules(() =>
                    {
                        validator
                            .RuleFor(x => x.Address.Street)
                            .NotEmpty()
                            .MinimumLength(2)
                            .MaximumLength(100)
                            .WithMessage("Street is required.");

                        validator
                            .RuleFor(x => x.Address.HouseNumber)
                            .NotEmpty()
                            .Matches(@"^[1-9]\w*")
                            .WithMessage("HouseNumber is required.");
                        validator
                            .RuleFor(x => x.Address.City)
                            .NotEmpty()
                            .MinimumLength(2)
                            .MaximumLength(50)
                            .WithMessage("City is required.");
                        validator
                            .RuleFor(x => x.Address.PostalCode)
                            .NotEmpty()
                            .Matches(@"^[1-9]\d{3}$")
                            .WithMessage("PostalCode is required.");
                    });
            }
        }

        public sealed class Create : BaseDto
        {
            public string? Auth0UserId { get; set; }

            public sealed class Validator : AbstractValidator<Create>
            {
                public Validator()
                {
                    ApplyBaseRules(this);
                    RuleFor(x => x.Auth0UserId)
                        .NotEmpty()
                        .WithMessage("Auth0 User ID is required.");
                }
            }
        }

        public sealed class Edit : BaseDto
        {
            public int Id { get; set;}

            public sealed class Validator : AbstractValidator<Edit>
            {
                public Validator()
                {
                    ApplyBaseRules(this);
                }
            }
        }

    }
}
