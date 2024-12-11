using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Rise.Shared.Bookings;
using Rise.Shared.Prices;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;
using Xunit.Abstractions;

namespace Rise.Client.Pages.Settings;

public class TimeSlotsShould : TestContext
{
    private readonly ITimeSlotService _timeSlotServiceMock;
    private readonly IUserService _userServiceMock;
    private readonly IPriceService _priceServiceMock;
    private readonly AuthenticationStateProvider _authStateProviderMock;
    private readonly IDialogService _dialogServiceMock;
    private readonly IBookingService _bookingServiceMock;

    public TimeSlotsShould(ITestOutputHelper outputHelper)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup mock services
        _timeSlotServiceMock = Substitute.For<ITimeSlotService>();
        _userServiceMock = Substitute.For<IUserService>();
        _authStateProviderMock = Substitute.For<AuthenticationStateProvider>();
        _dialogServiceMock = Substitute.For<IDialogService>();
        _bookingServiceMock = Substitute.For<IBookingService>();
        _priceServiceMock = Substitute.For<IPriceService>();

        var authState = new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "test-user") }, "test"))
        );
        _authStateProviderMock.GetAuthenticationStateAsync().Returns(authState);
        _userServiceMock.GetUserAsync(Arg.Any<string>()).Returns(new UserDto.Index { Id = 1 });
        _priceServiceMock.GetPriceAsync().Returns(new PriceDto.Index { Id = 1, Amount = 30 });

        // Register services
        Services.AddScoped(_ => _timeSlotServiceMock);
        Services.AddScoped(_ => _userServiceMock);
        Services.AddScoped(_ => _authStateProviderMock);
        Services.AddScoped(_ => _dialogServiceMock);
        Services.AddScoped(_ => _bookingServiceMock);
        Services.AddScoped(_ => _priceServiceMock);

        Services.AddMudServices(options =>
        {
            options.PopoverOptions.CheckForPopoverProvider = false;
        });

        // Setup default user
        _userServiceMock.GetUserAsync(Arg.Any<string>()).Returns(new UserDto.Index { Id = 1 });
    }

    [Fact]
    public void RenderTimeSlotPage()
    {
        // Arrange & Act
        var cut = RenderComponent<Index>();

        // Assert
        Assert.NotNull(cut.Find("h5"));
        Assert.NotNull(cut.Find("button")); // View toggle buttons
    }

    [Fact]
    public void ShowCalendarInCalendarView()
    {
        // Arrange
        var cut = RenderComponent<Index>();

        // Act - click calendar view button
        cut.FindAll("button")[1].Click();

        // Assert
        Assert.NotNull(cut.Find(".mud-picker"));
    }

    [Fact]
    public void ShowPrice()
    {
        // Arrange
        var cut = RenderComponent<Index>();

        // Assert
        Assert.NotNull(cut.Find("input[id=newPrice]"));
    }

    [Fact]
    public async Task ShowTimeSlotsInListView()
    {
        // Arrange
        var timeSlots = new List<TimeSlotDto>
        {
            new TimeSlotDto
            {
                Date = DateTime.Today,
                TimeSlot = 0,
                CreatedByUserId = 1,
                Reason = "Test",
            },
        };
        _timeSlotServiceMock
            .GetAllTimeSlotsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(timeSlots);

        var cut = RenderComponent<Index>();

        // Act - click list view button
        cut.FindAll("button")[0].Click();

        // Assert
        await Task.Delay(1000);
        Assert.NotNull(cut.Find("tbody"));
        Assert.NotNull(cut.Find("td"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ShowTimeSlotsInCalendarView(int timeSlotType)
    {
        // Arrange
        var timeSlots = new List<TimeSlotDto>
        {
            new TimeSlotDto
            {
                Date = DateTime.Today,
                TimeSlot = timeSlotType,
                CreatedByUserId = 1,
                Reason = "Test",
            },
        };
        _timeSlotServiceMock
            .GetAllTimeSlotsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(timeSlots);

        var cut = RenderComponent<Index>();

        // Act - click calendar view button
        cut.FindAll("button")[1].Click();

        // Assert
        await Task.Delay(1000);
        Assert.NotNull(cut.Find(".mud-picker"));
        Assert.NotNull(cut.Find(".mud-picker-calendar"));
        Assert.NotNull(cut.Find(".mud-picker-calendar-day"));
    }
}
