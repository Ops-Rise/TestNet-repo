using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Rise.Shared.Users;

namespace Rise.Client;

public partial class App
{
    [Inject]
    private IUserService UserService { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await CheckUserRegistrationAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await CheckUserRegistrationAsync();
    }

    private async Task CheckUserRegistrationAsync()
    {
        try
        {
            var authState = await authenticationStateTask;
            var user = authState.User;
            string currentUrl = NavigationManager.Uri;
            bool isAdmin = user.IsInRole("Administrator");

            if (user.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var auth0UserId = user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(auth0UserId))
            {
                throw new InvalidOperationException("Auth0 user ID not found.");
            }

            UserDto.Index? userDto = await UserService.GetUserAsync(auth0UserId!)!;

            if (userDto == null)
            {
                //new user
                string email = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value!;
                await UserService.CreateUserWithMailAsync(auth0UserId!, email);
                //set userId in UserService
                await UserService.GetUserIdAsync(auth0UserId!);
                NavigationManager.NavigateTo("/registration");
            }
            //set userId in UserService
            await UserService.GetUserIdAsync(auth0UserId!);

            var requiresRegistration =
                userDto?.IsRegistrationComplete == false
                && !currentUrl.Contains("/privacybeleid")
                && !currentUrl.Contains("/algemene_voorwaarden");

            if (requiresRegistration)
            {
                NavigationManager.NavigateTo("/registration");
            }
        }
        catch (Exception ex)
        {
            NavigationManager.NavigateTo("/error");
        }
    }
}
