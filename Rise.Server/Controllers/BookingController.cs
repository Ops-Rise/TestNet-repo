using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.Bookings;
using Rise.Shared.Exceptions;
using Rise.Shared.Payments;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;

namespace Rise.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController(
    IBookingService bookingService,
    IUserService userService,
    ILogger<BookingController> logger,
    IPaymentService paymentService
) : ControllerBase
{
    private readonly IBookingService _bookingService = bookingService;
    private readonly IUserService _userService = userService;
    private readonly IPaymentService paymentService = paymentService;
    private readonly ILogger<BookingController> _logger = logger;

    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    private const string BookingNotFoundMessage = "Booking not found.";

    private const string NoBookingFoundMessage = "No booking in the system found with id \"{0}\".";

    private const string BookingsNotFoundMessage = "No bookings found.";

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    /// <param name="booking">The booking details to create.</param>
    /// <returns>The ID of the newly created booking.</returns>
    /// <response code="201">Returns the newly created booking ID.</response>
    /// <response code="400">If a booking already exists with specified details or the input is invalid.</response>
    /// <response code="403">If the user is not authorized to create a booking.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    /// authorisatie in body
    [HttpPost]
    public async Task<IActionResult> CreateNewBooking(
        [FromBody] BookingDto.Mutate booking,
        [FromHeader(Name = "X-Redirect-Base")] string frontendBaseUrl
    )
    {
        _logger.LogInformation(
            "POST request received for creating a new booking with rentalDateTime \"{Date}\", boatId \"{BoatId}\", batteryId \"{BatteryId}\", userId \"{UserId}\", priceId \"{PriceId}\" and remark \"{Remark}\".",
            booking.RentalDateTime,
            booking.BoatId,
            booking.BatteryId,
            booking.UserId,
            booking.PriceId,
            booking.Remark
        );

        try
        {
            if (!await IsUserAuthorized(booking.UserId))
            {
                _logger.LogWarning("User is not authorized to create the booking.");
                return Forbid(); // 403 Forbidden
            }

            var totalAmount = await _bookingService.CalculateTotalAmountForBooking(booking);

            var (bookingId, _) = await _bookingService.CreateBookingAsync(booking);

            var paymentUrl = "/bookings";

            if (frontendBaseUrl != "dummyPayment")
            {
                paymentUrl = await paymentService.CreatePayment(
                    new PaymentRequestDto(
                        "Booking for user:" + booking.UserId,
                        totalAmount,
                        frontendBaseUrl + $"/bookings/{bookingId}"
                    ),
                    booking.UserId.ToString(),
                    bookingId
                );
            }

            _logger.LogInformation("Booking created with ID {BookingId}.", bookingId);

            return CreatedAtAction(
                nameof(CreateNewBooking),
                new { id = bookingId },
                new { PaymentUrl = "paymentUrl", BookingId = bookingId }
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message); // 400 Bad Request
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message); // 400 Bad Request
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error occurred while creating a new booking with rentalDateTime \"{Date}\", boatId \"{BoatId}\", batteryId \"{BatteryId}\", userId \"{UserId}\", priceId \"{PriceId}\" and remark \"{Remark}\".",
                booking.RentalDateTime,
                booking.BoatId,
                booking.BatteryId,
                booking.UserId,
                booking.PriceId,
                booking.Remark
            );
            return StatusCode(500, new { message = "Unexpected error occurred." }); // 500 Internal Server Error
        }
    }

    /// <summary>
    /// Haalt alle boekingen chronologisch op.
    /// </summary>
    /// <response code="200">Geeft een lijst van boekingen (BookingDto.Index) terug.</response>
    /// <response>403 Forbidden</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDto.Index>>> GetAllBookings()
    {
        _logger.LogInformation("GET request received for retrieving all bookings.");
        try
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            if (bookings == null || !bookings.Any())
            {
                _logger.LogWarning(BookingsNotFoundMessage);
                return NotFound(new { Message = BookingsNotFoundMessage });
            }

            logger.LogInformation("Successfully retrieved {Count} bookings.", bookings.Count());
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving bookings.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt alle boekingen op die nog moeten plaatsvinden.
    /// </summary>
    /// <response code="200">Geeft een lijst van boekingen (BookingDto.Index) terug.</response>
    /// <response code="500">Onverwachte fout</response>
    [HttpGet("current")]
    public async Task<ActionResult<IEnumerable<BookingDto.Index>>> GetCurrentBookings()
    {
        _logger.LogInformation("GET request received for retrieving all current bookings.");
        try
        {
            var bookings = await _bookingService.GetAllCurrentBookingsAsync();
            if (bookings == null || !bookings.Any())
            {
                _logger.LogWarning(BookingsNotFoundMessage);
                return NotFound(new { Message = BookingsNotFoundMessage });
            }

            logger.LogInformation(
                "Successfully retrieved {Count} current bookings.",
                bookings.Count()
            );
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving current bookings.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt alle tijdsloten op die volledig geboekt zijn.
    /// </summary>
    /// <response code="200">Geeft een lijst van DateTime objecten terug.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>

    [HttpGet("booked")]
    public async Task<ActionResult<IEnumerable<TimeSlotDto>>> GetFullyBookedBookings()
    {
        _logger.LogInformation("GET request received for retrieving all fully booked timeSlots.");
        try
        {
            var fullyBookedTimeSlots = await _bookingService.GetCurrentFullyBookedSlots();
            if (fullyBookedTimeSlots == null || !fullyBookedTimeSlots.Any())
            {
                _logger.LogWarning("No fully booked timeSlots found.");
                return NotFound(new { Message = "No fully booked timeSlots found." });
            }

            logger.LogInformation(
                "Successfully retrieved {Count} fully booked timeSlots.",
                fullyBookedTimeSlots.Count()
            );
            return Ok(fullyBookedTimeSlots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving fully booked timeSlots.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt de boeking met meegegeven id op.
    /// </summary>
    /// <param name="bookingId">De id van de boeking</param>
    /// <response code="200">Geeft de boeking (BookingDto.Detail) terug.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>
    /// authorisatie in body
    [HttpGet("{bookingId}")]
    public async Task<ActionResult<BookingDto.Detail>> GetBookingById(int bookingId)
    {
        _logger.LogInformation(
            "GET request received for retrieving booking with ID {BookingId}.",
            bookingId
        );
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            if (booking == null)
            {
                _logger.LogWarning(NoBookingFoundMessage, bookingId);

                return NotFound(new { message = BookingNotFoundMessage });
            }

            if (booking!.User != null && !await IsUserAuthorized(booking!.User.Id))
            {
                _logger.LogWarning("User is not authorized to view the booking.");
                return Forbid();
            }

            _logger.LogInformation(
                "Successfully retrieved booking with ID {BookingId}.",
                bookingId
            );
            return Ok(booking);
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
                "Error occurred while retrieving booking with ID {BookingId}.",
                bookingId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Haalt de boeking met meegegeven userId chronologisch op.
    /// </summary>
    ///  <param name="userId">userId van de gebruiker</param>
    /// <response code="200">Geeft de boeking (BookingDto.Detail) terug.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>
    //authorisatie in body
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetBookingsByUserIdAsync(int userId)
    {
        _logger.LogInformation(
            "GET request received for retrieving bookings with userId {UserId}.",
            userId
        );
        try
        {
            if (!await IsUserAuthorized(userId))
            {
                _logger.LogWarning(
                    "User is not authorized to view the bookings with userId {UserId}.",
                    userId
                );
                return Forbid();
            }

            var bookings = await _bookingService.GetBookingsByUserIdAsync(userId);

            if (bookings == null || !bookings.Any())
            {
                _logger.LogWarning(BookingsNotFoundMessage);
                return NotFound(new { Message = BookingsNotFoundMessage });
            }

            _logger.LogInformation(
                "Successfully retrieved {Count} bookings with userId {UserId}.",
                bookings.Count(),
                userId
            );
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving bookings with userId {UserId}.",
                userId
            );
            return StatusCode(500, new { Message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Update een specifieke boeking.
    /// </summary>
    /// <param name="bookingId">Id van de te updaten boeking</param>
    /// <param name="BookingDto.Mutate">De geüpdatete boeking</param>
    /// <response code="204">gelukt</response>
    /// <response code ="403">Forbidden</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpPut("{bookingId}")]
    public async Task<ActionResult> UpdateBooking(
        int bookingId,
        [FromBody] BookingDto.Mutate editModel
    )
    {
        _logger.LogInformation(
            "PUT request received for updating booking with ID {BookingId} with this data: rentalDateTime \"{Date}\", boatId \"{BoatId}\", batteryId \"{BatteryId}\", userId \"{UserId}\", priceId \"{PriceId}\" and remark {Remark}.",
            bookingId,
            editModel.RentalDateTime,
            editModel.BoatId,
            editModel.BatteryId,
            editModel.UserId,
            editModel.PriceId,
            editModel.Remark
        );
        try
        {
            var result = await _bookingService.UpdateBookingAsync(bookingId, editModel);
            if (!result)
            {
                _logger.LogWarning(NoBookingFoundMessage, bookingId);

                return NotFound(new { Message = BookingNotFoundMessage });
            }

            _logger.LogInformation("Successfully updated booking with ID {BookingId}.", bookingId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while updating booking with ID {BookingId} with this data: rentalDateTime \"{Date}\", boatId \"{BoatId}\", batteryId \"{BatteryId}\", userId \"{UserId}\", priceId \"{PriceId}\" and remark {Remark}.",
                bookingId,
                editModel.RentalDateTime,
                editModel.BoatId,
                editModel.BatteryId,
                editModel.UserId,
                editModel.PriceId,
                editModel.Remark
            );
            return StatusCode(500, new { Message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Annuleert een boeking en stuurt e-mail naar gebruiker.
    /// </summary>
    /// <param name="bookingId">Id van de te annuleren boeking</param>
    /// <response code="204">gelukt</response>
    /// <response code ="403">Forbidden</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>
    /// authorisatie in body
    [HttpPut("cancel/{bookingId}")]
    public async Task<ActionResult> CancelBooking(int bookingId)
    {
        _logger.LogInformation(
            "PUT request received for canceling booking with ID {BookingId}.",
            bookingId
        );
        try
        {
            BookingDto.Detail? booking = await _bookingService.GetBookingByIdAsync(bookingId);
            if (booking != null && !await IsUserAuthorized(booking.User.Id))
            {
                _logger.LogWarning(
                    "User is not authorized to cancel the booking with ID {BookingId}.",
                    bookingId
                );
                return Forbid();
            }

            var bookingCanceled = await _bookingService.CancelBookingAsync(bookingId);
            if (!bookingCanceled)
            {
                _logger.LogWarning(NoBookingFoundMessage, bookingId);
                return NotFound(new { Message = BookingNotFoundMessage });
            }

            if (booking.Remark != "testRemark")
            {
                try
                {
                    await paymentService.CancelPaymentById(bookingId);
                    _logger.LogInformation(
                        "Successfully canceled payment for booking ID {BookingId}.",
                        bookingId
                    );
                }
                catch (PaymentNotFoundException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "No payment found for booking ID {BookingId}.",
                        bookingId
                    );
                }
            }

            _logger.LogInformation("Successfully canceled booking with ID {BookingId}.", bookingId);
            return NoContent();
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
                "Error occurred while canceling booking with ID {BookingId}.",
                bookingId
            );
            return StatusCode(500, new { Message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Verwijdert de boeking met meegegeven id
    /// </summary>
    /// <param name="bookingId">De id van de boeking</param>
    /// <response code="204">Succesvol verwijderd</response>
    /// <response code ="403">Forbidden</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Onverwachte fout</response>
    [Authorize(Roles = "Administrator")]
    [HttpDelete("{bookingId}")]
    public async Task<ActionResult> DeleteBooking(int bookingId)
    {
        _logger.LogInformation(
            "DELETE request received for deleting booking with ID {BookingId}.",
            bookingId
        );
        try
        {
            var result = await _bookingService.DeleteBookingAsync(bookingId);
            if (!result)
            {
                _logger.LogWarning(NoBookingFoundMessage, bookingId);

                return NotFound(new { Message = BookingNotFoundMessage });
            }

            _logger.LogInformation("Successfully deleted booking with ID {BookingId}.", bookingId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while deleting booking with ID {BookingId}.",
                bookingId
            );
            return StatusCode(500, new { Message = UnexpectedErrorMessage });
        }
    }

    private async Task<bool> IsUserAuthorized(int userId)
    {
        if (User.IsInRole("Administrator"))
        {
            return true;
        }
        else
        {
            // Verkrijg de gebruiker die momenteel is ingelogd
            var auth0UserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (auth0UserIdClaim == null)
            {
                return false;
            }

            string? auth0UserId = await _userService.GetAuth0UserIdByUserId(userId);
            //als user bookings van andere users opvragen  => Forbid
            if (auth0UserId != auth0UserIdClaim)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
