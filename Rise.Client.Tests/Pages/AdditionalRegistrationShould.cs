using System;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Rise.Shared.Addresses;
using Rise.Shared.Users;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Client.Pages;

public class AdditionalRegistrationShould : TestContext
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IUserService _userServiceMock;
    private readonly AuthenticationStateProvider _authenticationStateProviderMock;

    public AdditionalRegistrationShould(ITestOutputHelper outputHelper)
    {
        //Services.AddXunitLogger(outputHelper);
        _outputHelper = outputHelper;

        JSInterop.Mode = JSRuntimeMode.Loose;

        _userServiceMock = Substitute.For<IUserService>();
        _authenticationStateProviderMock = Substitute.For<AuthenticationStateProvider>();

        Services.AddScoped(_ => _userServiceMock);
        Services.AddScoped(_ => _authenticationStateProviderMock);

        Services.AddMudServices(options =>
        {
            options.PopoverOptions.CheckForPopoverProvider = false;
        });
    }

    [Fact]
    public void RegistrationForm_RendersCorrectly()
    {
        // Arrange & Act
        var component = RenderComponent<AdditionalRegistration>();

        // Assert
        Assert.NotNull(component.Find("form"));
        Assert.NotNull(component.Find("input[id=voornaam]"));
        Assert.NotNull(component.Find("input[id=achternaam]"));
        Assert.NotNull(component.Find("input[id=straatnaam]"));
        Assert.NotNull(component.Find("input[id=nummer]"));
        Assert.NotNull(component.Find("input[id=bus]"));
        Assert.NotNull(component.Find("input[id=postcode]"));
        Assert.NotNull(component.Find("input[id=plaats]"));
        Assert.NotNull(component.Find("input[id=telefoon]"));
        Assert.NotNull(component.Find("input[id=geboortedatum]"));
        Assert.NotNull(component.Find("input[id=voorwaarden]"));
        Assert.NotNull(component.Find("input[id=privacy]"));
        Assert.NotNull(component.Find("button[type='submit']"));
    }

    [Fact]
    public void RegistrationForm_Validation_ShowsErrorMessages_WhenFieldsAreEmpty()
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();

        // Assert
        Assert.DoesNotContain("Voornaam is verplicht.", component.Markup);
        Assert.DoesNotContain("Achternaam is verplicht.", component.Markup);
        Assert.DoesNotContain("Straatnaam is verplicht.", component.Markup);
        Assert.DoesNotContain("Huisnummer is verplicht.", component.Markup);
        Assert.DoesNotContain("Postcode is verplicht.", component.Markup);
        Assert.DoesNotContain("Plaats is verplicht.", component.Markup);
        Assert.DoesNotContain("Telefoonnummer is verplicht.", component.Markup);
        Assert.DoesNotContain("Geboortedatum is verplicht.", component.Markup);

        // Act
        component.Find("form").Submit();

        // Assert
        Assert.Contains("Voornaam is verplicht.", component.Markup);
        Assert.Contains("Achternaam is verplicht.", component.Markup);
        Assert.Contains("Straatnaam is verplicht.", component.Markup);
        Assert.Contains("Huisnummer is verplicht.", component.Markup);
        Assert.Contains("Postcode is verplicht.", component.Markup);
        Assert.Contains("Plaats is verplicht.", component.Markup);
        Assert.Contains("Telefoonnummer is verplicht.", component.Markup);
        Assert.Contains("Geboortedatum is verplicht.", component.Markup);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("x")]
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenFirstnameIsTooShort(
        string firstname
    )
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();
        var firstnameField = component.Find("input[id='voornaam']");

        // Act
        firstnameField.Change(firstname);
        component.Find("form").Submit();

        // Assert
        Assert.Contains("Voornaam moet minstens 2 karakters bevatten.", component.Markup);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("x")]
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenLastnameIsTooShort(
        string lastname
    )
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();
        var lastnameField = component.Find("input[id='achternaam']");

        // Act
        lastnameField.Change(lastname);
        component.Find("form").Submit();

        // Assert
        Assert.Contains("Achternaam moet minstens 2 karakters bevatten.", component.Markup);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("x")]
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenStreetIsTooShort(string street)
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();
        var streetField = component.Find("input[id='straatnaam']");

        // Act
        streetField.Change(street);
        component.Find("form").Submit();

        // Assert
        Assert.Contains("Straat moet minstens 2 karakters bevatten.", component.Markup);
    }

    [Theory]
    [InlineData("012")]
    [InlineData("a5")]
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenInvalidHouseNumber(
        string houseNumber
    )
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();
        var houseNumberField = component.Find("input[id='nummer']");

        // Act
        houseNumberField.Change(houseNumber);
        component.Find("form").Submit();

        // Assert
        Assert.Contains("Huisnummer moet beginnen met een cijfer van 1 tot 9.", component.Markup);
    }

    [Theory]
    [InlineData("120")]
    [InlineData("5a")]
    public void RegistrationForm_Validation_ShowsNoErrorMessage_WhenValidHouseNumber(
        string houseNumber
    )
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();
        var houseNumberField = component.Find("input[id='nummer']");

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
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenPostalCodeIsInValid(
        string postalCode
    )
    {
        // Arrange
        var cut = RenderComponent<AdditionalRegistration>();
        var postalCodeField = cut.Find("input[id='postcode']");

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
    public void RegistrationForm_Validation_ShowsNoErrorMessage_WhenPostalCodeIsValid(
        string postalCode
    )
    {
        // Arrange
        var cut = RenderComponent<AdditionalRegistration>();
        var postalCodeField = cut.Find("input[id='postcode']");

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
    [InlineData("A")]
    [InlineData("x")]
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenCityIsTooShort(string city)
    {
        // Arrange
        var component = RenderComponent<AdditionalRegistration>();
        var cityField = component.Find("input[id='plaats']");

        // Act
        cityField.Change(city);
        component.Find("form").Submit();

        // Assert
        Assert.Contains("Plaats moet minstens 2 karakters bevatten.", component.Markup);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("04123")]
    [InlineData("+321234567890")]
    [InlineData("+420123456789")]
    [InlineData("0398372255")]
    public void RegistrationForm_Validation_ShowsErrorMessage_WhenPhoneNumberIsInvalid(
        string phoneNumber
    )
    {
        // Arrange
        var cut = RenderComponent<AdditionalRegistration>();
        var phoneField = cut.Find("input[id='telefoon']");

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
    public void RegistrationForm_Validation_ShowsNoErrorMessage_WhenPhoneNumberIsvalid(
        string phoneNumber
    )
    {
        // Arrange
        var cut = RenderComponent<AdditionalRegistration>();
        var phoneField = cut.Find("input[id='telefoon']");

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
    public void RegistrationForm_Validation_Passes_WhenAllFieldsAreValid()
    {
        // Arrange
        var navMan = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<AdditionalRegistration>();
        cut.Find("input[id='voornaam']").Change("John");
        cut.Find("input[id='achternaam']").Change("Doe");
        cut.Find("input[id='straatnaam']").Change("Hoofdstraat");
        cut.Find("input[id='nummer']").Change("12");
        cut.Find("input[id='postcode']").Change("1234");
        cut.Find("input[id='plaats']").Change("Brussel");
        cut.Find("input[id='telefoon']").Change("0471234567");
        cut.Find("input[id='geboortedatum']").Change("2000-01-01");

        // Act
        cut.Find("form").Submit();
        // Assert
        Assert.Equal("http://localhost/", navMan.Uri); // Redirected to home page
    }
}
