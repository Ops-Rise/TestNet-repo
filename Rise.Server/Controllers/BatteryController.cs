using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Domain.Batteries;
using Rise.Shared.Batteries;

namespace Rise.Server.Controllers;

//Only for administrators, todo peter en meter as well?
[Authorize(Roles = "Administrator")]
[ApiController]
[Route("api/[controller]")]
public class BatteryController(IBatteryService batteryService, ILogger<BatteryController> logger)
    : ControllerBase
{
    private readonly IBatteryService _batteryService = batteryService;
    private readonly ILogger<BatteryController> _logger = logger;
    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    /// <summary>
    /// Haalt alle batterijen op.
    /// </summary>
    /// <response code="200">Geeft een lijst van batterijen terug.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Geen batterijen gevonden</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BatteryDto.BatteryIndex>>> Get()
    {
        _logger.LogInformation("GET request received for retrieving all batteries.");
        try
        {
            var batteries = await _batteryService.GetAllBatteriesAsync();
            if (batteries == null || !batteries.Any())
            {
                _logger.LogWarning("No batteries found in the system.");
                return NotFound(new { Message = "No batteries found." });
            }
            _logger.LogInformation("Successfully retrieved {Count} batteries.", batteries.Count());
            return Ok(batteries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving batteries.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt een batterij op via het ID.
    /// </summary>
    /// <param name="batteryId">Het ID van de batterij.</param>
    /// <response code="200">Geeft de batterij met het opgegeven ID terug.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Batterij niet gevonden</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet("{batteryId}")]
    public async Task<ActionResult<BatteryDto.BatteryIndex>> Get(int batteryId)
    {
        _logger.LogInformation(
            "GET request received for retrieving battery with ID: \"{BatteryId}\".",
            batteryId
        );
        try
        {
            var battery = await _batteryService.GetBatteryByIdAsync(batteryId);
            if (battery is null)
            {
                _logger.LogWarning(
                    "No battery found in the system with id \"{BatteryId}\"",
                    batteryId
                );
                return NotFound(new { Message = $"No battery found with {batteryId}." });
            }
            _logger.LogInformation(
                "Successfully retrieved battery with id \"{BatteryId}\".",
                batteryId
            );
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving battery with id \"{BatteryId}\".",
                batteryId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt alle batterijen op met gedetailleerde informatie.
    /// </summary>
    /// <response code="200">Geeft een lijst van batterijen met details terug.</response>
    /// <response>403 Forbidden</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet("details")]
    public async Task<ActionResult<IEnumerable<BatteryDto.BatteryDetail>>> GetWithDetails()
    {
        _logger.LogInformation("GET request received for retrieving all batteries with details.");
        try
        {
            var batteries = await _batteryService.GetAllBatteriesWithDetailsAsync();
            if (batteries == null || !batteries.Any())
            {
                _logger.LogWarning("No batteries found in the system.");
                return NotFound(new { Message = "No batteries found." });
            }
            _logger.LogInformation(
                "Successfully retrieved {Count} batteries with details.",
                batteries.Count()
            );
            return Ok(batteries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving batteries with details.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt een batterij op met gedetailleerde informatie via het ID.
    /// </summary>
    /// <param name="batteryId">Het ID van de batterij.</param>
    /// <response code="200">Geeft de batterij met het opgegeven ID en details terug.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Batterij niet gevonden</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet("details/{batteryId}")]
    public async Task<ActionResult<BatteryDto.BatteryDetail>> GetWithDetails(int batteryId)
    {
        _logger.LogInformation(
            "GET request received for retrieving details of battery with ID: \"{BatteryId}\".",
            batteryId
        );
        try
        {
            var battery = await _batteryService.GetBatteryWithDetailsByIdAsync(batteryId);
            if (battery is null)
            {
                _logger.LogWarning(
                    "No battery found in the system with id \"{BatteryId}\"",
                    batteryId
                );
                return NotFound(new { message = $"No battery found with {batteryId}." });
            }
            _logger.LogInformation(
                "Successfully retrieved details of battery with id \"{BatteryId}\".",
                batteryId
            );
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving details of battery with id {BatteryId}",
                batteryId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Maakt een nieuwe batterij aan.
    /// </summary>
    /// <param name="model">De gegevens van de batterij om aan te maken.</param>
    /// <response code="201">Batterij succesvol aangemaakt.</response>
    /// <response code="400">Ongeldige batterijgegevens.</response>
    /// <response>403 Forbidden</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpPost]
    public async Task<ActionResult<int>> Post([FromBody] BatteryDto.Create model)
    {
        _logger.LogInformation(
            "POST request received for creating a new battery with name \"{Name}\" and userId \"{UserId}\".",
            model.Name,
            model.UserId
        );
        try
        {
            var batteryId = await _batteryService.CreateBatteryAsync(model);
            _logger.LogInformation(
                "Battery created successfully with id \"{BatteryId}\".",
                batteryId
            );
            return CreatedAtAction(nameof(Get), new { batteryId }, batteryId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while creating a new battery with name \"{Name}\" and userId \"{UserId}\".",
                model.Name,
                model.UserId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt batterijen op via status.
    /// </summary>
    /// <param name="status">De status van de batterijen om op te halen.</param>
    /// <response code="200">Geeft een lijst van batterijen met de opgegeven status terug.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Geen batterijen gevonden</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<BatteryDto.BatteryIndex>>> GetByStatus(
        BatteryStatus status
    )
    {
        _logger.LogInformation(
            "GET request received for retrieving all batteries with status \"{Status}\".",
            status.ToString()
        );
        try
        {
            var batteries = await _batteryService.GetBatteriesByStatusAsync(status);
            if (batteries == null || !batteries.Any())
            {
                _logger.LogWarning(
                    "No batteries found in the system with id \"{Status}\".",
                    status.ToString()
                );
                return NotFound(new { Message = "No batteries found." });
            }
            _logger.LogInformation(
                "Batteries recieved successfully with status \"{Status}\".",
                status.ToString()
            );
            return Ok(batteries);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving batteries with status \"{Status}\".",
                status.ToString()
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Wijzigt een bestaande batterij.
    /// </summary>
    /// <param name="batteryId">Het ID van de batterij om te wijzigen.</param>
    /// <param name="model">De gewijzigde batterijgegevens.</param>
    /// <response code="200">Batterij succesvol gewijzigd.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Batterij niet gevonden.</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpPut("{batteryId}")]
    public async Task<IActionResult> Put(int batteryId, [FromBody] BatteryDto.Mutate model)
    {
        _logger.LogInformation(
            "PUT request received for updating battery with id \"{BatteryId}\".",
            batteryId
        );
        try
        {
            await _batteryService.UpdateBatteryAsync(batteryId, model);
            _logger.LogInformation(
                "Battery updated successfully with id \"{BatteryId}\".",
                batteryId
            );
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while updating battery with id \"{BatteryId}\".",
                batteryId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Deletes a battery by ID.
    /// </summary>
    /// <param name="batteryId">The ID of the battery to delete.</param>
    /// <response code="204">Successfully deleted.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Battery not found.</response>
    /// <response code="500">Unexpected error occurred.</response>
    [HttpDelete("{batteryId}")]
    public async Task<ActionResult> Delete(int batteryId)
    {
        _logger.LogInformation(
            "DELETE request received for deleting battery with id \"{BatteryId}\".",
            batteryId
        );
        try
        {
            bool isDeleted = await _batteryService.DeleteBatteryAsync(batteryId);
            if (isDeleted)
            {
                _logger.LogInformation(
                    "Battery deleted successfully with id \"{BatteryId}\".",
                    batteryId
                );
                return NoContent();
            }
            _logger.LogInformation(
                "No battery found in the system with id \"{BatteryId}\".",
                batteryId
            );
            return NotFound(new { Message = $"No battery found with {batteryId}." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while deleting battery with id \"{BatteryId}\".",
                batteryId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }
}
