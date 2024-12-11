using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.Prices;

namespace Rise.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceController(IPriceService PriceService, ILogger<PriceController> logger)
    : ControllerBase
{
    private readonly IPriceService _PriceService = PriceService;
    private readonly ILogger<PriceController> _logger = logger;
    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    /// <summary>
    /// Haalt alle prijzen op met moment van creatie.
    /// </summary>
    /// <response code="200">Geeft een lijst van prijzen terug.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceDto.History>>> GetAllPrices()
    {
        _logger.LogInformation("GET request received for retrieving all prices.");
        try
        {
            var Prices = await _PriceService.GetAllPricesAsync();
            if (Prices == null || !Prices.Any())
            {
                _logger.LogWarning("No prices found.");
                return NotFound();
            }
            _logger.LogInformation("Prices successfully retrieved.");
            return Ok(Prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving prices.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt de meest recente prijs op.
    /// </summary>
    /// <returns>De meest recenste prijs.</returns>
    /// <response code="200">De meest recenste prijs</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Onverwachte fout</response>

    [HttpGet("latest")]
    public async Task<ActionResult<PriceDto.Index>> GetPrice()
    {
        _logger.LogInformation("GET request received for retrieving the latest price.");
        try
        {
            var Price = await _PriceService.GetPriceAsync();

            if (Price is null)
            {
                _logger.LogWarning("Latest price not found.");
                return NotFound();
            }
            _logger.LogInformation("Latest price successfully retrieved.");
            return Ok(Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving the latest price.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt een prijs op aan de hand van id.
    /// </summary>
    /// <param name="priceId">De id van de op te halen prijs</param>
    /// <returns>De prijs met opgegeven id.</returns>
    /// <response code="200">De prijs met opgegeven id</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpGet("{priceId}")]
    public async Task<ActionResult<PriceDto.Index>> GetPriceById(int priceId)
    {
        _logger.LogInformation(
            "GET request received for retrieving price with id {PriceId}.",
            priceId
        );
        try
        {
            var Price = await _PriceService.GetPriceByIdAsync(priceId);

            if (Price is null)
            {
                _logger.LogWarning("Price with id {PriceId} not found.", priceId);
                return NotFound();
            }
            _logger.LogInformation("Price with id {PriceId} successfully retrieved.", priceId);
            return Ok(Price);
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
                "An unexpected error occurred while retrieving price with id {PriceId}.",
                priceId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Creëert een nieuwe prijs
    /// </summary>
    /// <param name="createDto">DTO object met de nieuwe prijs</param>
    /// <returns>id van aangemaakte prijs.</returns>
    /// <response code="201">id van aangemaakte prijs.</response>
    /// <response>403 Forbidden</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<ActionResult<int>> CreateNewPrice([FromBody] PriceDto.Create createDto)
    {
        _logger.LogInformation(
            "POST request received for creating a new price with amount {Amount}.",
            createDto.Amount
        );
        try
        {
            if (createDto == null)
            {
                _logger.LogError("Price data is required.");
                return BadRequest("Prijs data is verplicht.");
            }
            var createdPriceId = await _PriceService.CreatePriceAsync(createDto);
            if (createdPriceId == 0)
            {
                _logger.LogError("Price data is required.");
                return BadRequest("Prijs data is verplicht.");
            }
            _logger.LogInformation("Price with id {PriceId} successfully created.", createdPriceId);
            return createdPriceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while creating a new price with amount {Amount}.",
                createDto.Amount
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }
}
