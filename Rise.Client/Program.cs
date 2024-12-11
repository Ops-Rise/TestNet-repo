using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
// Addition of mudblazor
using MudBlazor.Services;
using Rise.Client;
using Rise.Client.Auth;
using Rise.Client.Bookings;
using Rise.Client.Services;
using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;
using Rise.Shared.ProfileImages;
using Rise.Shared.Prices;
using Rise.Shared.Emails;
using Smart.Blazor;


var BUUT_API = "BuutAPI";
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

builder.Services.AddSingleton<ProfileStateService>();

// Configure HttpClient for the API
builder
    .Services.AddHttpClient(
        BUUT_API,
        client =>
        {
            var baseUrl = builder.Configuration["ApiSettings:BackendUrl"];
            client.BaseAddress = new Uri(baseUrl!);
        }
    )
    .AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

// Register the Services

builder.Services.AddScoped<IBookingService, BookingService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API);
    var navigationManager = sp.GetRequiredService<NavigationManager>();

    return new BookingService(httpClient, navigationManager);
});

builder.Services.AddScoped<IBoatService, BoatService>(sp => new BoatService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

builder.Services.AddScoped<IBatteryService, BatteryService>(sp => new BatteryService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

builder.Services.AddScoped<IProfileImageService, ProfileImageService>(sp => new ProfileImageService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

builder.Services.AddScoped<ITimeSlotService, TimeSlotService>(sp => new TimeSlotService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

builder.Services.AddScoped<IPriceService, PriceService>(sp => new PriceService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

builder.Services.AddScoped<IEmailService, EmailService>(sp => new EmailService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(BUUT_API)
));

// Set up authentication
builder.Services.AddCascadingAuthenticationState();
builder
    .Services.AddOidcAuthentication(options =>
    {
        builder.Configuration.Bind("Auth0", options.ProviderOptions);
        options.ProviderOptions.ResponseType = "code";
        options.ProviderOptions.PostLogoutRedirectUri = builder.HostEnvironment.BaseAddress;
        options.ProviderOptions.AdditionalProviderParameters.Add(
            "audience",
            builder.Configuration["Auth0:Audience"]!
        );
    })
    .AddAccountClaimsPrincipalFactory<ArrayClaimsPrincipalFactory<RemoteUserAccount>>();

// Addition of mudblazor
builder.Services.AddMudServices();
builder.Services.AddSmart();

await builder.Build().RunAsync();
