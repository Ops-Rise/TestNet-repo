using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.TimeSlots;

namespace Rise.Server.Controllers;

/// <summary>
/// Controller voor het beheren van tijdsloten voor boekingen
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimeSlotController(ITimeSlotService timeSlotService, ILogger<TimeSlotController> logger)
    : ControllerBase
{
    private readonly ITimeSlotService _timeSlotService = timeSlotService;
    private readonly ILogger<TimeSlotController> _logger = logger;

    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    /// <summary>
    /// Haalt alle geblokkeerde tijdsloten binnen een bepaalde periode op
    /// </summary>
    /// <param name="startDate">Startdatum van de periode </param>
    /// <param name="endDate">Einddatum van de periode </param>
    /// <returns>Lijst van geblokkeerde tijdsloten met hun details</returns>
    /// <response code="200">Lijst van tijdsloten succesvol opgehaald</response>
    /// /// <response code="400">Ongeldige query parameters</response>
    /// <response code="401">Niet geautoriseerd - gebruiker moet ingelogd zijn als beheerder</response>
    /// <response code="500">Interne serverfout</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimeSlotDto>>> GetTimeSlots(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate
    )
    {
        _logger.LogInformation("GET request received for retrieving all timeslots.");
        if (startDate > endDate)
        {
            _logger.LogError("Start date is after end date.");
            return BadRequest("Startdatum moet voor einddatum liggen");
        }

        if ((endDate - startDate).TotalDays > 365)
        {
            _logger.LogError("Date range is greater than 1 year.");
            return BadRequest("Datumbereik mag niet groter zijn dan 1 jaar");
        }

        if (startDate.Date < DateTime.Today)
        {
            _logger.LogError("Start date is in the past.");
            return BadRequest("Startdatum mag niet in het verleden liggen");
        }
        try
        {
            var timeSlots = await _timeSlotService.GetAllTimeSlotsAsync(startDate, endDate);

            _logger.LogInformation("Succesfully retrieved {Count} timeslots.", timeSlots.Count());    
            return Ok(timeSlots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while receiving all timeslots.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Blokkeert een specifiek tijdslot voor boekingen
    /// </summary>
    /// <param name="model">Details van het te blokkeren tijdslot</param>
    /// <remarks>
    /// Tijdslot types:
    /// - 0 = Ochtend (09:00)
    /// - 1 = Middag (12:00)
    /// - 2 = Namiddag (15:00)
    /// </remarks>
    /// <response code="200">Tijdslot succesvol geblokkeerd</response>
    /// <response code="400">Tijdslot kan niet worden geblokkeerd omdat er al een boeking bestaat</response>
    /// <response code="401">Niet geautoriseerd - gebruiker moet ingelogd zijn als beheerder</response>
    [Authorize(Roles = "Administrator")]
    [HttpPost("block")]
    public async Task<ActionResult> BlockTimeSlot(TimeSlotDto model)
    {
        _logger.LogInformation("POST request received for blocking a timeslot.");
        if (model == null)
        {
            _logger.LogError("No timeslot data received.");
            return BadRequest("Geen tijdslot gegevens ontvangen");
        }
        if (model.Date.Date < DateTime.Today)
        {
            _logger.LogError("Date is in the past.");
            return BadRequest("Tijdslot datum mag niet in het verleden liggen");
        }
        try
        {
            await _timeSlotService.BlockTimeSlotAsync(model);
            _logger.LogInformation("Timeslot successfully blocked.");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while blocking a timeslot.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Deblokkeert een  geblokkeerd tijdslot
    /// </summary>
    /// <param name="date">Datum van het tijdslot</param>
    /// <param name="timeSlot">Type tijdslot (0 = Ochtend, 1 = Middag, 2 = Namiddag)</param>
    /// <response code="200">Tijdslot succesvol gedeblokkeerd</response>
    /// <response code="401">Niet geautoriseerd - gebruiker moet ingelogd zijn als beheerder</response>
    [Authorize(Roles = "Administrator")]
    [HttpDelete("unblock")]
    public async Task<ActionResult> UnblockTimeSlot(
        [FromQuery] DateTime date,
        [FromQuery] int timeSlot
    )
    {
        _logger.LogInformation("DELETE request received for unblocking a timeslot.");
        if (date.Date < DateTime.Today)
        {
            _logger.LogError("Date is in the past.");
            return BadRequest("Tijdslot datum mag niet in het verleden liggen");
        }

        try
        {
            await _timeSlotService.UnblockTimeSlotAsync(date, timeSlot);
            _logger.LogInformation("Timeslot successfully unblocked.");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while unblocking a timeslot.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }
}
