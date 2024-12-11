using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Domain.Boats;
using Rise.Shared.Boats;

namespace Rise.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BoatController(IBoatService boatService, ILogger<BoatController> logger)
    : ControllerBase
{
    private readonly IBoatService _boatService = boatService;
    private readonly ILogger<BoatController> _logger = logger;
    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    /// <summary>
    /// Haalt alle boten op.
    /// </summary>
    /// <response code="200">Geeft een lijst van boten terug.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoatDto.BoatIndex>>> GetAllBoats()
    {
        _logger.LogInformation("GET request received for retrieving all boats.");
        try
        {
            var boats = await _boatService.GetAllBoatsAsync();
            if (boats == null || !boats.Any())
            {
                _logger.LogInformation("No boats found in the system.");
                return NotFound();
            }
            logger.LogInformation("Successfully retrieved {Count} boats.", boats.Count());
            return Ok(boats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving boats.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt het aantal beschikbare boten op.
    /// </summary>
    /// <response code="200">Geeft het aantal beschikbare boten terug.</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet("available/count")]
    public async Task<ActionResult<int>> GetNumberOfAvailableBoatsAsync()
    {
        _logger.LogInformation(
            "GET request received for retrieving the number of available boats."
        );
        try
        {
            var availableBoatsCount = await _boatService.GetAvailableBoatsCountAsync();
            _logger.LogInformation(
                "Successfully retrieved the number of available boats: {Count}.",
                availableBoatsCount
            );
            return Ok(availableBoatsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving the number of available boats.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Retrieves a boat by its ID.
    /// </summary>
    /// <param name="boatId">The ID of the boat to retrieve.</param>
    /// <returns>The boat with the specified ID.</returns>
    /// <response code="200">Returns the requested boat.</response>
    /// <response code="404">If the boat with the specified ID is not found.</response>
    /// <response code="500">Onverwachte fout</response>

    [HttpGet("{boatId}")]
    public async Task<ActionResult<BoatDto.BoatIndex>> GetBoatById(int boatId)
    {
        _logger.LogInformation(
            "GET request received for retrieving boat with ID \"{BoatId}\".",
            boatId
        );
        try
        {
            var boat = await _boatService.GetBoatByIdAsync(boatId);

            if (boat is null)
            {
                _logger.LogWarning("No boat in the system found with id \"{BoatId}\".", boatId);
                return NotFound();
            }
            _logger.LogInformation("Successfully retrieved boat with ID \"{BoatId}\".", boatId);
            return Ok(boat);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving boat with ID \"{BoatId}\".",
                boatId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Updates the status of an existing boat.
    /// </summary>
    /// <param name="boatId">The ID of the boat to update.</param>
    /// <param name="updateDto">The updated status of the boat.</param>
    /// <returns>The updated boat details.</returns>
    /// <response code="200">Returns the updated boat details.</response>
    /// <response code="400">If the provided boat status is invalid.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Onverwachte fout</response>
    /// <response code="404">If the boat with the specified ID is not found.</response>
    [Authorize(Roles = "Administrator")]
    [HttpPut("{boatId}")]
    public async Task<ActionResult<BoatDto.BoatIndex>> UpdateBoatStatus(
        int boatId,
        [FromBody] BoatDto.Mutate model
    )
    {
        _logger.LogInformation(
            "PUT request received for updating the status of boat with ID \"{BoatId}\".",
            boatId
        );
        try
        {
            if (!Enum.IsDefined(typeof(BoatStatus), model.Status))
            {
                _logger.LogError("Invalid boat status \"{Status}\".", model.Status.ToString());
                return BadRequest("Invalid boat status.");
            }

            var updatedBoat = await _boatService.UpdateBoatStatusAsync(boatId, model);
            if (updatedBoat == null)
            {
                _logger.LogWarning("No boat in the system found with id \"{BoatId}\".", boatId);
                return NotFound();
            }
            _logger.LogInformation(
                "Successfully updated the status of boat with ID \"{BoatId}\".",
                boatId
            );
            return Ok(updatedBoat);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while updating the status of boat with ID \"{BoatId}\".",
                boatId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Creates a new boat.
    /// </summary>
    /// <param name="createDto">The details of the boat to create.</param>
    /// <returns>The created boat details.</returns>
    /// <response code="201">Returns the newly created boat.</response>
    /// <response>403 Forbidden</response>
    /// <response code="400">If the input data is invalid.</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<ActionResult<BoatDto.BoatIndex>> CreateNewBoat(
        [FromBody] BoatDto.CreateBoatDto createDto
    )
    {
        _logger.LogInformation(
            "POST request received for creating a new boat with name \"{Name}\" and status \"{Status}\".",
            createDto.Name,
            createDto.Status.ToString()
        );
        try
        {
            if (createDto == null)
            {
                _logger.LogError("Boat data is required.");
                return BadRequest("Boat data is required.");
            }
            var createdBoat = await _boatService.CreateNewBoatAsync(createDto);
            if (createdBoat == null)
            {
                _logger.LogError("Failed to create the boat.");
                return BadRequest("Failed to create the boat.");
            }
            _logger.LogInformation(
                "Successfully created a new boat with ID \"{BoatId}\".",
                createdBoat.Id
            );
            return Created(
                string.Empty,
                new BoatDto.BoatIndex { Id = createdBoat.Id, Name = createdBoat.Name }
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while creating a new boat with name \"{Name}\" and status \"{Status}\".",
                createDto.Name,
                createDto.Status.ToString()
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Soft-deletes a boat by ID.
    /// </summary>
    /// <param name="boatId">The ID of the boat to delete.</param>
    /// <response code="204">The boat was successfully deleted.</response>
    /// <response code="404">If the boat with the specified ID is not found.</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpDelete("{boatId}")]
    public async Task<ActionResult> DeleteBoat(int boatId)
    {
        _logger.LogInformation(
            "DELETE request received for deleting boat with ID \"{BoatId}\".",
            boatId
        );
        try
        {
            var result = await _boatService.DeleteBoatAsync(boatId);
            if (!result)
            {
                _logger.LogWarning("No boat in the system found with id \"{BoatId}\".", boatId);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted boat with ID \"{BoatId}\".", boatId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while deleting boat with ID \"{BoatId}\".",
                boatId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }
}
