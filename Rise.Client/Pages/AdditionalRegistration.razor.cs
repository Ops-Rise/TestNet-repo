using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Shared.Addresses;
using Rise.Shared.Users;

namespace Rise.Client.Pages;

public partial class AdditionalRegistration
{
    private RegisterAccountForm model = new RegisterAccountForm
    {
        Firstname = string.Empty,
        Lastname = string.Empty,
        Street = string.Empty,
        HouseNumber = string.Empty,
        PostalCode = string.Empty,
        City = string.Empty,
        PhoneNumber = string.Empty,
        BirthDay = null,
    };
    private bool success;

    private async Task OnValidSubmit(EditContext context)
    {
        success = true;

        //auth0UserId
        AuthenticationState authState =
            await AuthenticationStateProvider.GetAuthenticationStateAsync();
        string auth0UserId = authState.User.FindFirst("sub")?.Value!;

        UserDto.Create userDto =
            new()
            {
                Auth0UserId = auth0UserId,
                Firstname = model.Firstname,
                Lastname = model.Lastname,
                PhoneNumber = model.PhoneNumber,
                BirthDay = model.BirthDay,
                Address = new AddressDto
                {
                    Street = model.Street,
                    HouseNumber = model.HouseNumber,
                    UnitNumber = model.UnitNumber,
                    PostalCode = model.PostalCode,
                    City = model.City,
                },
            };

        await UserService.CompleteUserRegistrationAsync(userDto);

        NavigationManager.NavigateTo("/bookings");
    }
}
