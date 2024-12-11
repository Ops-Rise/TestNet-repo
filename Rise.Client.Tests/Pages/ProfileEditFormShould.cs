using System;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudBlazor.Services;
using System.Threading.Tasks;
using NSubstitute;
using Rise.Client.Services;
using Rise.Shared.Addresses;
using Rise.Shared.Users;
using Rise.Shared.ProfileImages;
using Xunit;
using Xunit.Abstractions;
using System.IO;


namespace Rise.Client.Pages.Profile
{

    public class ProfileEditFormShould : TestContext
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly IUserService _userServiceMock;
        private readonly IProfileImageService _profileImageServiceMock;
        private readonly ProfileStateService _profileStateServiceMock;
        private readonly AuthenticationStateProvider _authenticationStateProviderMock;

        public ProfileEditFormShould(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

            JSInterop.Mode = JSRuntimeMode.Loose;

            _userServiceMock = Substitute.For<IUserService>();
            _profileImageServiceMock = Substitute.For<IProfileImageService>();
            _authenticationStateProviderMock = Substitute.For<AuthenticationStateProvider>();
            _profileStateServiceMock = Substitute.For<ProfileStateService>();

            Services.AddScoped(_ => _userServiceMock);
            Services.AddScoped(_ => _profileImageServiceMock);
            Services.AddScoped(_ => _authenticationStateProviderMock);
            Services.AddScoped(_ => _profileStateServiceMock);

            Services.AddMudServices(options =>
            {
                options.PopoverOptions.CheckForPopoverProvider = false;
            });
        }

        private void SetupUserProfile()
        {
            // Mock user profile with default values
            var userProfile = new UserDto.Index
            {
                Id = 1,
                Firstname = "John",
                Lastname = "Doe",
                BirthDay = DateTime.Parse("1990-01-01"),
                Email = "john.doe@example.com",
                PhoneNumber = "0471234567",
                Address = new AddressDto { Street = "Hoofdstraat", HouseNumber = "1", PostalCode = "1000", City = "Brussel", UnitNumber = "3" }
            };
            var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", "userId")
            })));

            _authenticationStateProviderMock.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));
            _userServiceMock.GetUserAsync("userId").Returns(Task.FromResult(userProfile));

            //Create a mock base64-encoded image
            var mockProfileImage = new ProfileImageDto.Detail
            {
                Id = 1,
                UserId = 1,
                ContentType = "image/png",
                ImageBlob = new byte[] { 0x89, 0x50, 0x4E, 0x47 } // Example image data
            };

            _profileImageServiceMock.GetProfileImageAsync(1).Returns(Task.FromResult(mockProfileImage));
        }

        [Fact]
        public void EditForm_RendersCorrectly()
        {
            SetupUserProfile();
            // Arrange & Act
            var component = RenderComponent<Edit>();

            // Assert
            Assert.NotNull(component.Find("form"));
            Assert.NotNull(component.Find("input[id=Voornaam]"));
            Assert.NotNull(component.Find("input[id=Achternaam]"));
            Assert.NotNull(component.Find("input[id=Straat]"));
            Assert.NotNull(component.Find("input[id=Huisnummer]"));
            Assert.NotNull(component.Find("input[id=Busnummer]"));
            Assert.NotNull(component.Find("input[id=Postcode]"));
            Assert.NotNull(component.Find("input[id=Stad]"));
            Assert.NotNull(component.Find("input[id=Telefoonnummer]"));
            Assert.NotNull(component.Find("input[id=Geboortedatum]"));
            Assert.NotNull(component.Find("button[type='submit']"));
        }

        [Fact]
        public void EditForm_Validation_ShowsErrorMessages_WhenFieldsAreEmpty()
        {
            SetupUserProfile();
            // Arrange
            var component = RenderComponent<Edit>();

            // Assert
            Assert.DoesNotContain("Voornaam is verplicht.", component.Markup);
            Assert.DoesNotContain("Achternaam is verplicht.", component.Markup);
            Assert.DoesNotContain("Straatnaam is verplicht.", component.Markup);
            Assert.DoesNotContain("Huisnummer is verplicht.", component.Markup);
            Assert.DoesNotContain("Postcode is verplicht.", component.Markup);
            Assert.DoesNotContain("Telefoonnummer is verplicht.", component.Markup);
            Assert.DoesNotContain("Geboortedatum is verplicht.", component.Markup);

            component.Find("input[id='Voornaam']").Change("");
            component.Find("input[id='Achternaam']").Change("");
            component.Find("input[id='Straat']").Change("");
            component.Find("input[id='Huisnummer']").Change("");
            component.Find("input[id='Postcode']").Change("");
            component.Find("input[id='Telefoonnummer']").Change("");
            component.Find("input[id='Geboortedatum']").Change("");

            // Act
            component.Find("form").Submit();

            // Assert
            Assert.Contains("Voornaam is verplicht.", component.Markup);
            Assert.Contains("Achternaam is verplicht.", component.Markup);
            Assert.Contains("Straatnaam is verplicht.", component.Markup);
            Assert.Contains("Huisnummer is verplicht.", component.Markup);
            Assert.Contains("Postcode is verplicht.", component.Markup);
            Assert.Contains("Telefoonnummer is verplicht.", component.Markup);
            Assert.Contains("Geboortedatum is verplicht.", component.Markup);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("x")]
        public void EditForm_Validation_ShowsErrorMessage_WhenFirstnameIsTooShort(
            string firstname
        )
        {
            SetupUserProfile();
            // Arrange
            var component = RenderComponent<Edit>();
            var firstnameField = component.Find("input[id='Voornaam']");

            // Act
            firstnameField.Change(firstname);
            component.Find("form").Submit();

            // Assert
            Assert.Contains("Voornaam moet minstens 2 karakters bevatten.", component.Markup);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("x")]
        public void EditForm_Validation_ShowsErrorMessage_WhenLastnameIsTooShort(
            string lastname
        )
        {
            SetupUserProfile();
            // Arrange
            var component = RenderComponent<Edit>();
            var lastnameField = component.Find("input[id='Achternaam']");

            // Act
            lastnameField.Change(lastname);
            component.Find("form").Submit();

            // Assert
            Assert.Contains("Achternaam moet minstens 2 karakters bevatten.", component.Markup);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("x")]
        public void EditForm_Validation_ShowsErrorMessage_WhenStreetIsTooShort(string street)
        {
            SetupUserProfile();
            // Arrange
            var component = RenderComponent<Edit>();
            var streetField = component.Find("input[id='Straat']");

            // Act
            streetField.Change(street);
            component.Find("form").Submit();

            // Assert
            Assert.Contains("Straat moet minstens 2 karakters bevatten.", component.Markup);
        }

        [Theory]
        [InlineData("012")]
        [InlineData("a5")]
        public void EditForm_Validation_ShowsErrorMessage_WhenInvalidHouseNumber(
            string houseNumber
        )
        {
            SetupUserProfile();
            // Arrange
            var component = RenderComponent<Edit>();
            var houseNumberField = component.Find("input[id='Huisnummer']");

            // Act
            houseNumberField.Change(houseNumber);
            component.Find("form").Submit();

            // Assert
            Assert.Contains("Huisnummer moet beginnen met een cijfer van 1 tot 9.", component.Markup);
        }

        [Theory]
        [InlineData("120")]
        [InlineData("5a")]
        public void EditForm_Validation_ShowsNoErrorMessage_WhenValidHouseNumber(
            string houseNumber
        )
        {
            SetupUserProfile();
            // Arrange
            var component = RenderComponent<Edit>();
            var houseNumberField = component.Find("input[id='Huisnummer']");

            // Act
            houseNumberField.Change(houseNumber);
            component.Find("form").Submit();

            // Assert
            Assert.DoesNotContain(
                "Huisnummer moet beginnen met een cijfer van 1 tot 9.",
                component.Markup
            );
        }

        [Theory]
        [InlineData("98765")]
        [InlineData("10001")]
        [InlineData("999")]
        [InlineData("123")]
        [InlineData("0123")]
        public void EditForm_Validation_ShowsErrorMessage_WhenPostalCodeIsInValid(
            string postalCode
        )
        {
            SetupUserProfile();
            // Arrange
            var cut = RenderComponent<Edit>();
            var postalCodeField = cut.Find("input[id='Postcode']");

            // Act
            postalCodeField.Change(postalCode);
            cut.Find("form").Submit();

            // Assert
            Assert.Contains("Postcode moet exact 4 cijfers bevatten, niet starten met 0.", cut.Markup);
        }

        [Theory]
        [InlineData("1000")]
        [InlineData("9999")]
        [InlineData("5453")]
        public void EditForm_Validation_ShowsNoErrorMessage_WhenPostalCodeIsValid(
            string postalCode
        )
        {
            SetupUserProfile();
            // Arrange
            var cut = RenderComponent<Edit>();
            var postalCodeField = cut.Find("input[id='Postcode']");

            // Act
            postalCodeField.Change(postalCode);
            cut.Find("form").Submit();

            // Assert
            Assert.DoesNotContain(
                "Postcode moet exact 4 cijfers bevatten, niet starten met 0.",
                cut.Markup
            );
        }

        [Theory]
        [InlineData("123456789")]
        [InlineData("04123")]
        [InlineData("+321234567890")]
        [InlineData("+420123456789")]
        [InlineData("0398372255")]
        public void EditForm_Validation_ShowsErrorMessage_WhenPhoneNumberIsInvalid(
            string phoneNumber
        )
        {
            SetupUserProfile();
            // Arrange
            var cut = RenderComponent<Edit>();
            var phoneField = cut.Find("input[id='Telefoonnummer']");

            // Act
            phoneField.Change(phoneNumber);
            // Simuleer het indrukken van de Tab-toets
            var tabKey = new KeyboardEventArgs { Key = "Tab" };
            phoneField.TriggerEvent("onkeydown", tabKey);

            // Assert
            Assert.Contains(
                "Ongeldig Belgisch telefoonnummer. Het moet beginnen met 04 (10 cijfers) of +32 (11 cijfers).",
                cut.Markup
            );
        }

        [Theory]
        [InlineData("0412345678")]
        [InlineData("0499999999")]
        public void EditForm_Validation_ShowsNoErrorMessage_WhenPhoneNumberIsValid(
            string phoneNumber
        )
        {
            SetupUserProfile();
            // Arrange
            var cut = RenderComponent<Edit>();
            var phoneField = cut.Find("input[id='Telefoonnummer']");

            // Act
            phoneField.Change(phoneNumber);
            // Simuleer het indrukken van de Tab-toets
            var tabKey = new KeyboardEventArgs { Key = "Tab" };
            phoneField.TriggerEvent("onkeydown", tabKey);

            // Assert
            Assert.DoesNotContain(
                "Ongeldig Belgisch telefoonnummer. Het moet beginnen met 04 (10 cijfers) of +32 (11 cijfers).",
                cut.Markup
            );
        }

        [Fact]
        public void EditForm_Validation_Passes_WhenAllFieldsAreValid()
        {

            SetupUserProfile();
            // Arrange
            var navMan = Services.GetRequiredService<FakeNavigationManager>();
            var cut = RenderComponent<Edit>();
            cut.Find("input[id='Voornaam']").Change("John");
            cut.Find("input[id='Achternaam']").Change("Doe");
            cut.Find("input[id='Straat']").Change("Hoofdstraat");
            cut.Find("input[id='Huisnummer']").Change("15");
            cut.Find("input[id='Postcode']").Change("1000");
            cut.Find("input[id='Stad']").Change("Brussel");
            cut.Find("input[id='Telefoonnummer']").Change("0412345678");
            cut.Find("input[id='Geboortedatum']").Change("01/01/1990");

            // Act
            cut.Find("form").Submit();

            // Assert
            Assert.Equal("http://localhost/", navMan.Uri);
        }
    }
}