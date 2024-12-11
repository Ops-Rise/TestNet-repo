using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Bogus.DataSets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Rise.Client.Components;
using Rise.Client.Services;
using Rise.Shared.Bookings;
using Rise.Shared.Prices;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;

[assembly: SuppressMessage(
    "CodeQuality",
    "S1144:Unused private types or members should be removed",
    Scope = "type",
    Target = "~T:Rise.Client.Pages.Settings.Index"
)]

namespace Rise.Client.Pages.Settings;

public partial class Index : ComponentBase
{
    private bool _isLoading = true;

    private enum ViewType
    {
        List,
        Calendar,
    }

    private List<TimeSlotDto> _timeSlots = new();

    private IEnumerable<BookingDto.Index> _bookings = new List<BookingDto.Index>();

    private ViewType _currentView = ViewType.Calendar;
    private int currentPage = 0;

    private readonly int[] _pageSizeOptions = new int[] { 10, 25, 50 };

    private DateTime? Date { get; set; } = DateTime.Today;

    private readonly DateTime _today = DateTime.Today;

    private readonly DateTime _minDate = DateTime.Today;

    private readonly DateTime _maxDate = DateTime.Today.AddYears(1);

    private BookingDto.Index? firstFutureBooking;

    [Inject]
    public required ITimeSlotService TimeSlotService { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    public IUserService UserService { get; set; } = default!;

    [Inject]
    public IBookingService BookingService { get; set; } = default!;

    [Inject]
    public IPriceService PriceService { get; set; } = default!;

    private int userId;

    private string _blockReason = "Geblokkeerd";
    private decimal _price { get; set; } = default!;
    private PriceModel _model = new PriceModel { Amount = decimal.Zero };

    private readonly string _culturInfo = "nl-BE";

    private readonly int[] _availableSlots = new[] { 9, 12, 15 };

    protected override async Task OnInitializedAsync()
    {
        await LoadTimeSlots();
        await LoadBookings();
        await LoadPrice();

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(c => c.Type == "sub")?.Value;
        var accountUser = await UserService.GetUserAsync(userIdClaim);
        userId = accountUser.Id;
    }

    private async Task ChangeView(ViewType viewType)
    {
        _currentView = viewType;
        await LoadTimeSlots();
    }

    private async Task LoadBookings()
    {
        var bookingsResult = await BookingService.GetAllCurrentBookingsAsync();
        _bookings = bookingsResult ?? new List<BookingDto.Index>();
    }

    private (bool hasBookings, int count) GetBookingsForTimeSlot(DateTime date, int timeSlot)
    {
        var bookingsForSlot = _bookings.Count(b =>
            b.RentalDateTime.Date == date.Date && b.RentalDateTime.Hour == timeSlot
        );

        return (bookingsForSlot > 0, bookingsForSlot);
    }

    private async Task LoadPrice()
    {
        PriceDto.Index? priceDto = await PriceService.GetPriceAsync();
        _price = priceDto!.Amount;
    }

    private async Task OnDateChanged(DateTime? selectedDate)
    {
        Date = selectedDate;
        await Task.CompletedTask;
    }

    private static string FormatTimeSlot(int timeSlot)
    {
        return $"{timeSlot:D2}:00 ";
    }

    private async Task LoadTimeSlots()
    {
        _isLoading = true;

        var result = await TimeSlotService.GetAllTimeSlotsAsync(_today, _today.AddDays(100));
        _timeSlots =
            result?.OrderBy(x => x.Date).ThenBy(x => x.TimeSlot).ToList()
            ?? new List<TimeSlotDto>();

        _isLoading = false;
    }

    private async Task ShowConfirmationDialog(
        string title,
        string message,
        string confirmText,
        string cancelText,
        Func<Task> confirmAction,
        bool includeReasonInput = false,
        bool hasAdditionalMessage = false,
        Action<string>? onReasonChanged = null,
        string? AdditionalMessage = null
    )
    {
        var parameters = new DialogParameters
        {
            ["Title"] = title,
            ["Message"] = message,
            ["ConfirmText"] = confirmText,
            ["CancelText"] = cancelText,
            ["OnConfirmAction"] = EventCallback.Factory.Create(this, confirmAction),
            ["IncludeReasonInput"] = includeReasonInput,
            ["OnReasonChanged"] = onReasonChanged,
            ["HasAdditionalMessage"] = hasAdditionalMessage,
            ["AdditionalMessage"] = AdditionalMessage,
        };

        var options = new DialogOptions { CloseOnEscapeKey = true };

        var dialog = await DialogService.ShowAsync<GenericDialog>(title, parameters, options);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadTimeSlots();
        }
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var parameters = new DialogParameters
        {
            ["Title"] = title,
            ["Message"] = message,
            ["ConfirmText"] = "OK",
            ["ShowCancelButton"] = false,
            ["IncludeReasonInput"] = false,
        };

        var options = new DialogOptions { CloseOnEscapeKey = true };
        await DialogService.ShowAsync<GenericDialog>(title, parameters, options);
    }

    private async Task CreateTimeSlotForHour(int hour)
    {
        if (Date == null)
            return;
        var selectedDate = Date.Value.Date;
        var formattedDateTime = selectedDate.AddHours(hour);

        if (formattedDateTime < DateTime.Now)
        {
            await ShowErrorDialog(
                "Tijdslot blokkeren",
                "Het is niet mogelijk om tijdsloten in het verleden te blokkeren."
            );
            return;
        }

        var (hasBookings, bookingCount) = GetBookingsForTimeSlot(selectedDate, hour);

        string message =
            $"Wilt u het tijdslot blokkeren op {formattedDateTime.ToString("dddd dd/MM/yyyy 'om' HH:mm", new CultureInfo(_culturInfo))}?";
        string? extraMessage = null;
        if (hasBookings)
        {
            extraMessage =
                $"LET OP: Er {(bookingCount == 1 ? "is" : "zijn")} {bookingCount} actieve "
                + $"boeking{(bookingCount == 1 ? "" : "en")} voor dit tijdslot. "
                + $"Deze {(bookingCount == 1 ? "zal" : "zullen")} automatisch geannuleerd worden.";
        }

        await ShowConfirmationDialog(
            "Tijdslot blokkeren",
            message,
            "Blokkeren",
            "Annuleren",
            async () => await ConfirmBlockTimeSlot(hour, _blockReason),
            includeReasonInput: true,
            hasAdditionalMessage: hasBookings,
            onReasonChanged: reason => _blockReason = reason,
            AdditionalMessage: extraMessage
        );
    }

    private async Task ConfirmBlockTimeSlot(int hour, string reason)
    {
        Console.WriteLine("ConfirmBlockTimeSlot FOR USER ID: " + userId);

        var model = new TimeSlotDto
        {
            Date = Date.Value.Add(TimeSpan.Parse($"{hour}:00:00")),
            TimeSlot = hour,
            Reason = reason,
            CreatedByUserId = userId,
        };

        await TimeSlotService.BlockTimeSlotAsync(model);
        await LoadTimeSlots();
        await LoadBookings();
    }

    private async Task DeleteTimeSlot(TimeSlotDto timeSlot)
    {
        if (Date == null)
            return;

        if (timeSlot.Date < DateTime.Today)
        {
            await ShowErrorDialog(
                "Dag deblokkeren",
                "Het is niet mogelijk om tijdsloten in het verleden te deblokkeren."
            );
            return;
        }

        string message =
            $"Wilt u het tijdslot deblokkeren op {timeSlot.Date.ToString("dddd dd/MM/yyyy 'om' HH:mm", new CultureInfo(_culturInfo))}?";

        await ShowConfirmationDialog(
            "Tijdslot deblokkeren",
            message,
            "Deblokkeren",
            "Annuleren",
            async () => await ConfirmDeleteTimeSlot(timeSlot)
        );
    }

    private async Task ConfirmDeleteTimeSlot(TimeSlotDto timeSlot)
    {
        var success = await TimeSlotService.UnblockTimeSlotAsync(timeSlot.Date, timeSlot.TimeSlot);
        if (success)
        {
            await LoadTimeSlots();
        }
    }

    private string GetDateClass(DateTime date)
    {
        var blockedSlotsCount = _timeSlots.Count(t =>
            t.Date.Date == date.Date && _availableSlots.Contains(t.TimeSlot)
        );

        var hasBookings = _availableSlots.Any(slot =>
            GetBookingsForTimeSlot(date, slot).hasBookings
        );

        string className = string.Empty;
        if (blockedSlotsCount == _availableSlots.Length)
        {
            className = "fully-blocked";
        }
        else if (blockedSlotsCount > 0)
        {
            className = "partially-blocked";
        }

        if (hasBookings)
        {
            className += " has-bookings";
        }

        return className;
    }

    private async Task HandleBlockDay(int[] availableSlots)
    {
        if (Date == null)
            return;

        if (Date.Value.Date < DateTime.Today)
        {
            await ShowErrorDialog(
                "Dag Blokkeren",
                "Het is niet mogelijk om dagen in het verleden te blokkeren."
            );
            return;
        }

        var blockedSlots = _timeSlots
            .Where(t => t.Date.Date == Date.Value.Date && availableSlots.Contains(t.TimeSlot))
            .ToList();

        var allSlotsBlocked = blockedSlots.Count == availableSlots.Length;

        if (allSlotsBlocked)
        {
            string message =
                "Wilt u alle tijdsloten deblokkeren op "
                + Date.Value.ToString("dddd dd/MM/yyyy", new CultureInfo(_culturInfo))
                + "?";

            await ShowConfirmationDialog(
                "Dag Deblokkeren",
                message,
                "Deblokkeren",
                "Annuleren",
                async () => await UnblockAllSlots(blockedSlots)
            );
        }
        else
        {
            int totalBookings = 0;
            foreach (var slot in availableSlots)
            {
                var (_, count) = GetBookingsForTimeSlot(Date.Value, slot);
                totalBookings += count;
            }

            string message =
                $"Wilt u alle tijdsloten blokkeren op {Date.Value.ToString("dddd dd/MM/yyyy", new CultureInfo(_culturInfo))}?";

            string? extraMessage = null;
            if (totalBookings > 0)
            {
                extraMessage +=
                    $"\n\nLET OP: Er {(totalBookings == 1 ? "is" : "zijn")} {totalBookings} actieve "
                    + $"boeking{(totalBookings == 1 ? "" : "en")} voor deze dag. "
                    + $"Deze {(totalBookings == 1 ? "zal" : "zullen")} automatisch geannuleerd worden.";
            }
            await ShowConfirmationDialog(
                "Dag Blokkeren",
                message,
                "Blokkeren",
                "Annuleren",
                async () => await BlockEntireDay(blockedSlots, availableSlots, _blockReason),
                includeReasonInput: true,
                hasAdditionalMessage: totalBookings > 0,
                onReasonChanged: reason => _blockReason = reason,
                AdditionalMessage: extraMessage
            );
        }
    }

    private async Task BlockEntireDay(
        List<TimeSlotDto> existingBlocks,
        int[] allSlots,
        string reason
    )
    {
        _blockReason = reason;

        // First unblock any existing blocks
        if (existingBlocks.Any())
        {
            await UnblockAllSlots(existingBlocks);
        }

        // Then block all slots
        foreach (var hour in allSlots)
        {
            await ConfirmBlockTimeSlot(hour, _blockReason);
        }

        await LoadTimeSlots();
        await LoadBookings();
    }

    private async Task UnblockAllSlots(List<TimeSlotDto> blockedSlots)
    {
        foreach (var slot in blockedSlots)
        {
            await TimeSlotService.UnblockTimeSlotAsync(slot.Date, slot.TimeSlot);
        }

        await LoadTimeSlots();
    }

    private class PriceModel
    {
        [Required(ErrorMessage = "Prijs is verplicht.")]
        [RegularExpression(@"^\d+([,]\d{1,2})?$", ErrorMessage = "Max 2 cijfers na de komma.")]
        public required decimal Amount { get; set; }
    }

    private async Task OnValidSubmitNewPrice(EditContext context)
    {
        await ShowConfirmationDialog(
            "Nieuwe prijs",
            $"Wilt u de prijs op €{_model.Amount} instellen?",
            "Ja",
            "Nee",
            async () => await ConfirmNewPrice(_model.Amount)
        );
    }

    private async Task ConfirmNewPrice(decimal amount)
    {
        PriceDto.Create createDto = new PriceDto.Create { Amount = amount };
        await PriceService.CreatePriceAsync(createDto);
        _model = new PriceModel { Amount = decimal.Zero };
        await LoadPrice();
    }
}
