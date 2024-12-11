using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Domain.Bookings;
using Rise.Shared.Prices;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;
using Smart.Blazor;

[assembly: SuppressMessage(
    "CodeQuality",
    "S1144:Unused private types or members should be removed",
    Scope = "type",
    Target = "~T:Rise.Client.Components.DatePicker"
)]

namespace Rise.Client.Components;

public partial class DatePicker
{
    private DateTime? Date { get; set; } = DateTime.Today;
    private readonly DateTime _today = DateTime.Today;

    private List<TimeSlotDto> _timeSlots = new List<TimeSlotDto>();

    private static readonly DateTime maxDate = DateTime.Today.AddDays(30);

    private static readonly DateTime minDate = DateTime.Today.AddDays(3);
    private BookingDto.Mutate Booking { get; set; } = new();
    private string? SelectedHour { get; set; }

    private bool _showAlert = false;
    private bool _showDateAlert = false;
    string[] restrictedDates = new string[] { }; // Can be used to let the admin restrict certain dates in late development

    private string? AlertMessage { get; set; }

    [Parameter]
    public int BookingId { get; set; }

    [Inject]
    public IBookingService BookingService { get; set; } = default!;

    [Inject]
    public IBoatService BoatService { get; set; } = default!;

    [Inject]
    public IPriceService PriceService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    public IUserService UserService { get; set; } = default!;

    [Inject]
    public ITimeSlotService TimeSlotService { get; set; } = default!;

    public BookingDto.Detail? BookingDetail { get; set; }
    private int userId;

    private readonly List<string> availableHours = new List<string>
    {
        "09:00 ",
        "12:00 ",
        "15:00 ",
    };
    private IEnumerable<BookingDto.Index> _currentBookings { get; set; } =
        new List<BookingDto.Index>();

    private IEnumerable<DateTime> _fullyBookedDates { get; set; } = new List<DateTime>();
    private IEnumerable<DateTime> _partiallyBookedDates { get; set; } = new List<DateTime>();

    private IEnumerable<DateTime> _fullyBlockedDates { get; set; } = new List<DateTime>();

    private IEnumerable<DateTime> _partiallyBlockedDates { get; set; } = new List<DateTime>();

    private int _availableBoats;
    private PriceDto.Index? _priceDto { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var auth0UserId = authState.User.FindFirst(c => c.Type == "sub")?.Value;
        userId = await UserService.GetUserIdAsync(auth0UserId!)!;

        await LoadCurrentBookings();

        await LoadTimeSlots();
        await LoadAvailableBoats();
        await LoadPrice();
        Console.WriteLine("Available boats: " + _availableBoats);
        ProcessBookingDates(availableHours, _availableBoats);
    }

    private async Task LoadCurrentBookings()
    {

        var result = await BookingService.GetAllCurrentBookingsAsync();

        _currentBookings =
            result?.OrderBy(x => x.RentalDateTime).ToList() ?? new List<BookingDto.Index>();

    }

    private async Task LoadTimeSlots()
    {
        var result = await TimeSlotService.GetAllTimeSlotsAsync(minDate, maxDate);
        _timeSlots =
            result?.OrderBy(x => x.Date).ThenBy(x => x.TimeSlot).ToList()
            ?? new List<TimeSlotDto>();
    }

    private async Task LoadAvailableBoats()
    {
        _availableBoats = await BoatService.GetAvailableBoatsCountAsync();
        Console.WriteLine("Available boats: " + _availableBoats);
    }

    private async Task LoadPrice()
    {
        _priceDto = await PriceService.GetPriceAsync();
    }

    private void ProcessBookingDates(List<string> requiredHours, int availableBoats)
    {
        var requiredTimes = requiredHours.Select(time => TimeSpan.Parse(time.Trim())).ToList();

        var bookingsByDate = _currentBookings
            .GroupBy(b => b.RentalDateTime.Date)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.GroupBy(b => b.RentalDateTime.TimeOfDay)
                        .ToDictionary(timeGroup => timeGroup.Key, timeGroup => timeGroup.Count())
            );

        var timeslotsByDate = _timeSlots
            .GroupBy(b => b.Date.Date)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.GroupBy(b => b.Date.TimeOfDay)
                        .ToDictionary(timeGroup => timeGroup.Key, timeGroup => timeGroup.Count())
            );

        _fullyBookedDates = bookingsByDate
            .Where(g =>
                requiredTimes.TrueForAll(time =>
                    bookingsByDate[g.Key].ContainsKey(time)
                    && bookingsByDate[g.Key][time] >= availableBoats
                )
            )
            .Select(g => g.Key)
            .ToList();

        _partiallyBookedDates = bookingsByDate
            .Where(g =>
                requiredTimes.Exists(time =>
                    !bookingsByDate[g.Key].ContainsKey(time)
                    || bookingsByDate[g.Key][time] < availableBoats
                )
            )
            .Select(g => g.Key)
            .ToList();

        _fullyBlockedDates = timeslotsByDate
            .Where(g => requiredTimes.TrueForAll(time => timeslotsByDate[g.Key].ContainsKey(time)))
            .Select(g => g.Key)
            .ToList();

        _partiallyBlockedDates = timeslotsByDate
            .Where(g => requiredTimes.Exists(time => !timeslotsByDate[g.Key].ContainsKey(time)))
            .Select(g => g.Key)
            .ToList();
    }

    private bool IsDateDisabled(DateTime date)
    {
        int unusableHours = availableHours.Count(hour =>
            IsHourBlocked(date, hour, _availableBoats)
        );

        return _fullyBookedDates.Contains(date.Date)
            || _availableBoats == 0
            || _fullyBlockedDates.Contains(date.Date)
            || unusableHours == availableHours.Count;
    }

    private bool IsHourBlocked(DateTime date, string hour, int availableBoats)
    {
        TimeSpan time = TimeSpan.Parse(hour.Trim(), CultureInfo.InvariantCulture);

        var isTimeSlotBlocked = _timeSlots.Exists(ts =>
            ts.Date.Date == date.Date && ts.TimeSlot == time.Hours
        );

        var allreadyBookedByUser = _currentBookings.Any(b =>
            b.RentalDateTime.Date == date.Date
            && b.RentalDateTime.TimeOfDay == time
            && b.Status == BookingStatus.Active
            && b.User.Id == userId
        );

        if (isTimeSlotBlocked || allreadyBookedByUser)
        {
            return true;
        }

        if (_partiallyBlockedDates.Contains(date.Date))
        {
            var scopedTime = _timeSlots.Find(ts =>
                ts.Date.Date == date.Date && ts.TimeSlot == time.Hours
            );
            if (scopedTime != null)
            {
                return true;
            }
        }

        if (_partiallyBookedDates.Contains(date.Date))
        {
            int bookingsCount = _currentBookings.Count(b =>
                b.RentalDateTime.Date == date.Date && b.RentalDateTime.TimeOfDay == time
            );

            return bookingsCount >= availableBoats;
        }

        return false;
    }

    private async Task ConfirmBooking()
    {
        if (Date.HasValue && !string.IsNullOrEmpty(SelectedHour))
        {
            if (Date >= minDate && Date <= maxDate)
            {
                await LoadCurrentBookings();
                Booking.RentalDateTime = Date.Value.Add(TimeSpan.Parse(SelectedHour.Trim()));
                Booking.UserId = userId;
                int existingBookingsCount = _currentBookings.Count(b =>
                    b.RentalDateTime.Date == Booking.RentalDateTime.Date
                    && b.RentalDateTime.TimeOfDay == Booking.RentalDateTime.TimeOfDay
                    && b.Status == BookingStatus.Active
                );
                Booking.PriceId = _priceDto!.Id;

                if (existingBookingsCount < _availableBoats)
                {
                    var (bookingId,paymentUrl) = await BookingService.CreateBookingAsync(Booking);
                    BookingId = bookingId;
                    await LoadBooking();
                    await LoadCurrentBookings();
                    
                    NavigationManager.NavigateTo($"bookings?bookingId={BookingId}");

                    ShowConfirmAlert();
                }
                else
                {
                    string message = "Deze datum en uur zijn al volgeboekt";
                    ShowDateAlert(message);
                    Console.WriteLine("Selected date and time slot is fully booked.");
                }
            }
            else
            {
                Console.WriteLine("Date not within the allowed range.");
                string message =
                    $"Uw reservering moet na {minDate.AddDays(-1):dd/MM/yyyy} en voor {maxDate.AddDays(1):dd/MM/yyyy} vallen.";
                ShowDateAlert(message);
            }
        }
        else
        {
            Console.WriteLine("Date or hour not selected.");
        }
    }

    private string GetDateClass(DateTime date)
    {
        if (date < minDate || date > maxDate)
        {
            return "date-out-of-range";
        }

        if (IsDateDisabled(date))
        {
            return "date-booked";
        }

        return string.Empty;
    }

    private async Task LoadBooking()
    {
        BookingDetail = await BookingService.GetBookingByIdAsync(BookingId);
    }

    private void CloseAlert()
    {
        _showAlert = false;
        NavigationManager.NavigateTo($"/bookings/{BookingId}");
    }

    private void CloseDateAlert()
    {
        _showDateAlert = false;
    }

    private void ShowConfirmAlert()
    {
        _showAlert = true;
    }

    private void ShowDateAlert(string message)
    {
        _showDateAlert = true;
        AlertMessage = message;
    }

    private void SelectHour(string hour)
    {
        SelectedHour = hour;
        StateHasChanged();
    }

    private async Task OnDateChanged(DateTime? selectedDate)
    {
        SelectedHour = null;

        if (
            selectedDate.HasValue
            && !restrictedDates.Contains(selectedDate.Value.ToString("yyyy-MM-dd"))
        )
        {
            Date = selectedDate;
        }
        else
        {
            Date = null;
            ShowDateAlert("De geselecteerde datum is niet beschikbaar.");
        }

        await Task.CompletedTask;
    }

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    private async Task OpenDialogAsync()
    {
        if (Date.HasValue && !string.IsNullOrEmpty(SelectedHour))
        {
            Booking.RentalDateTime = Date.Value.Add(TimeSpan.Parse(SelectedHour.Trim()));

            var parameters = new DialogParameters
            {
                ["Title"] = "Boekingsgegevens",
                ["Message"] =
                    $"Bevestig uw reservering op {Booking.RentalDateTime.ToString("dddd dd/MM/yyyy 'om' HH:mm", new System.Globalization.CultureInfo("nl-BE"))}.",

                ["ConfirmText"] = "Bevestigen",
                ["CancelText"] = "Annuleren",
                ["OnConfirmAction"] = EventCallback.Factory.Create(this, ConfirmBooking),
            };

            var options = new DialogOptions { CloseOnEscapeKey = true };

            var dialog = DialogService.Show<ConfirmationDialog>(
                "Boekingsgegevens",
                parameters,
                options
            );
            var result = await dialog.Result;
        }
        else
        {
            Console.WriteLine("Date or hour not selected.");
        }
    }
}
