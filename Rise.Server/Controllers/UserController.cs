using System.Net.Http;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.Users;

namespace Rise.Server.Controllers;

[ApiController]
[Route("api/")]
public class UserController(IUserService userService, ILogger<UserController> logger)
    : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UserController> _logger = logger;
    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    /// <summary>
    /// Geeft alle gebruikers terug
    /// </summary>
    /// <response>200 and users (UserDto.Index)</response>
    /// <response>403 Forbidden</response>
    /// <response> 404 Not Found</response>
    /// <response>500 Internal Server Error</response>

    [Authorize(Roles = "Administrator")]
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto.Index>>> Get()
    {
        _logger.LogInformation("GET request received for retrieving all users.");
        try
        {
            var users = await _userService.GetAllAsync();

            if (users == null || !users.Any())
            {
                _logger.LogWarning("No users found.");
                return NotFound();
            }
            logger.LogInformation("Successfully retrieved {Count} users.", users.Count());
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving all users.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Geeft alle gebruikers terug die door het verificatieproces zijn gegaan en een opleidingsmoment hebben gevolgd.
    /// </summary>
    /// <response>200 and users (UserDto.Index)</response>
    /// <response>403 Forbidden</response>
    /// <response> 404 Not Found</response>
    /// <response>500 Internal Server Error</response>

    [Authorize(Roles = "Administrator")]
    [HttpGet("users/verified")]
    public async Task<ActionResult<IEnumerable<UserDto.Index>>> GetVerifiedUsersAsync()
    {
        _logger.LogInformation("GET request received for retrieving all users.");
        try
        {
            var users = await _userService.GetVerifiedUsersAsync();

            if (users == null || !users.Any())
            {
                _logger.LogWarning("No users found.");
                return NotFound();
            }
            logger.LogInformation("Successfully retrieved {Count} users.", users.Count());
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving all users.");
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Creëert een nieuwe gebruiker met meegegeven auth0UserId en email
    /// </summary>
    /// <param name="auth0UserId">Het auth0UserId van de gebruiker.</param>
    /// <param name="email">Het email van de gebruiker.</param>
    /// <response>id van de nieuwe gebruiker</response>
    /// <response code="400">A user already exists with the specified auth0UserId.</response>
    /// <response>403 Forbidden</response>
    /// <response code="500">Unexpected error</response>
    [Authorize(Policy = "OwnDataOrAdmin")]
    [HttpPost("register/{auth0UserId}")]
    public async Task<IActionResult> CreateUserWithMailAsync(
        string auth0UserId,
        [FromBody] string email
    )
    {
        _logger.LogInformation(
            "POST request received for creating a new user with auth0UserId \"{Auth0UserId}\" and email \"{Email}\".",
            auth0UserId,
            email
        );
        try
        {
            int userId = await _userService.CreateUserWithMailAsync(auth0UserId, email);
            _logger.LogInformation("User created with ID {UserId}.", userId);
            return Ok(userId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while creating a new user with auth0UserId \"{Auth0UserId}\" and email \"{Email}\".",
                auth0UserId,
                email
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Registreert de aanvullende registratiegegevens van een gebruiker.
    /// </summary>
    /// <param name="userDto">Een userDTO met de aanvullende registratiegegevens van de gebruiker.</param>
    /// <response code="200">Gegevens geregistreerd.</response>
    /// <response code="400">BadRequest</response>
    /// <response>403 Forbidden</response>
    /// <response>404 Not Found</response>
    /// <response code="500">Unexpected error</response>
    //authorisatie in body
    [HttpPost("register")]
    public async Task<IActionResult> CompleteUserRegistrationAsync(
        [FromBody] UserDto.Create userDto
    )
    {
        _logger.LogInformation(
            "POST request received for completing user registration with auth0UserId \"{Auth0UserId}\".",
            userDto.Auth0UserId
        );
        //Authorisatie
        if (!User.IsInRole("Administrator"))
        {
            // Verkrijg de gebruiker die momenteel is ingelogd
            var auth0UserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //userDto.Auth0UserId is de auth0UserId van de gebruiker die de request maakt
            if (auth0UserIdClaim == null || userDto.Auth0UserId != auth0UserIdClaim)
            {
                _logger.LogWarning(
                    "User with auth0UserId \"{Auth0UserId1}\" not authorized to complete registration for user with auth0UserId \"{Auth0UserId2}\".",
                    auth0UserIdClaim,
                    userDto.Auth0UserId
                );
                return Forbid();
            }
        }

        try
        {
            await _userService.CompleteUserRegistrationAsync(userDto);
            _logger.LogInformation(
                "User registration completed for user with auth0UserId \"{Auth0UserId}\".",
                userDto.Auth0UserId
            );
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while completing user registration with auth0UserId \"{Auth0UserId}\".",
                userDto.Auth0UserId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Geeft de gebruiker terug met de gegeven auth0UserId
    /// </summary>
    /// <param name="auth0UserId">De auth0UserId van de gebruiker.</param>
    /// <response>200 and UserDto.Index</response>
    /// <response>404 Not Found</response>
    /// <response>403 Forbidden</response>
    [Authorize(Policy = "OwnDataOrAdmin")]
    [HttpGet("users/{auth0UserId}")]
    public async Task<ActionResult<UserDto.Index>> GetUserAsync(string auth0UserId)
    {
        _logger.LogInformation(
            "GET request received for retrieving user with auth0UserId \"{Auth0UserId}\".",
            auth0UserId
        );
        try
        {
            UserDto.Index? user = await _userService.GetUserAsync(auth0UserId);
            if (user == null)
            {
                _logger.LogWarning(
                    "User with auth0UserId \"{Auth0UserId}\" not found.",
                    auth0UserId
                );
                return NotFound();
            }
            else
            {
                _logger.LogInformation(
                    "User with auth0UserId \"{Auth0UserId}\" found.",
                    auth0UserId
                );
                return Ok(user);
            }
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
                "An unexpected error occurred while retrieving user with auth0UserId \"{Auth0UserId}\".",
                auth0UserId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Update de registratie status van een gebruiker
    /// </summary>
    /// <param name="auth0UserId">De auth0UserId van de gebruiker.</param>
    /// <response>200</response>
    /// <response>400 userId not found</response>
    /// <response>500 Unexpected error</response>
    /// <response>403 Forbidden</response>
    [Authorize(Policy = "OwnDataOrAdmin")]
    [HttpPut("users/{auth0UserId}/registration-status")]
    public async Task<IActionResult> UpdateUserRegistrationStatus(
        string auth0UserId,
        [FromBody] bool isComplete
    )
    {
        _logger.LogInformation(
            "PUT request received for updating user registration status with auth0UserId \"{Auth0UserId}\" and bool \"{IsComplete}\".",
            auth0UserId,
            isComplete ? "true" : "false"
        );
        try
        {
            await _userService.UpdateUserRegistrationStatusAsync(auth0UserId, isComplete);
            _logger.LogInformation(
                "User registration status updated for user with auth0UserId \"{Auth0UserId}\".",
                auth0UserId
            );
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while updating user registration status with auth0UserId \"{Auth0UserId}\" and bool \"{IsComplete}\".",
                auth0UserId,
                isComplete ? "true" : "false"
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Updates a user by ID.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// /// <param name="user">The updated user data.</param>
    /// <response code="200">Returns the updated user.</response>
    /// <response code="400">Bad request.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Unexpected error occurred.</response>
    /// authorisatie in body
    [HttpPut("users/{userId}")]
    public async Task<ActionResult<UserDto.Index>> Update(int userId, [FromBody] UserDto.Edit user)
    {   
        _logger.LogInformation(
            "PUT request received for updating user with ID {UserId}.",
            userId
        );
        try
        {
            //Authorisatie
            if (!User.IsInRole("Administrator"))
            {
                // Verkrijg de gebruiker die momenteel is ingelogd
                var auth0UserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (auth0UserIdClaim == null)
                {
                   _logger.LogWarning("Forbidden");
                    return Forbid();
                }
                string? auth0UserId = await _userService.GetAuth0UserIdByUserId(userId);
                //als user andere user updaten => Forbid
                if (auth0UserId != auth0UserIdClaim)
                {
                    _logger.LogWarning("user with auth0UserId \"\"{Auth0UserId1} not authorized to update user with ID \"{UserId}\" and auth0UserId \"{Auth0UserId2}\".", auth0UserIdClaim, userId, auth0UserId);
                    return Forbid();
                }
            }

            var updatedUser = await userService.UpdateAsync(userId, user);
            _logger.LogInformation("User updated with ID {UserId}.", userId);
            return Ok(updatedUser);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating user with ID {UserId}.", userId);
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Update de trainingsstatus van een gebruiker.
    /// </summary>
    /// <param name="userId">De id van de gebruiker.</param>
    /// <param name="isTrainingComplete">Een boolean die de status van de training aangeeft.</param>
    /// <response code="200">Trainingstatus bijgewerkt.</response>
    /// <response code="400">Gebruiker met auth0UserId niet gevonden of andere fout.</response>
    /// <response code="403">Forbidden.</response>
    /// <response code="500">Onverwachte fout.</response>
    [Authorize(Roles = "Administrator")]
    [HttpPut("users/{userId}/activate")]
    public async Task<IActionResult> UpdateUserTrainingStatus(
        int userId,
        [FromBody] bool isTrainingComplete
    )
    {
        _logger.LogInformation(
            "PUT request received for updating user training status with ID {UserId} and bool \"{IsTrainingComplete}\".",
            userId,
            isTrainingComplete ? "true" : "false"
        );
        try
        {
            await _userService.UpdateUserTrainingStatusAsync(userId, isTrainingComplete);
            _logger.LogInformation(
                "User training status updated for user with ID {UserId}.",
                userId
            );
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while updating user training status with ID {UserId} and bool \"{IsTrainingComplete}\".",
                userId,
                isTrainingComplete ? "true" : "false"
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <response code="204">Successfully deleted.</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Unexpected error occurred.</response>
    [Authorize(Roles = "Administrator")]
    [HttpDelete("users/{userId}")]
    public async Task<ActionResult> Delete(int userId)
    {   
        _logger.LogInformation("DELETE request received for deleting user with ID {UserId}.", userId);
        try
        {
            bool deleted = await _userService.DeleteAsync(userId);
            if (deleted)
            {
                _logger.LogInformation("User deleted with ID {UserId}.", userId);
                return NoContent();
            }
            else
            {
                _logger.LogWarning("User with ID {UserId} not found.", userId);
                return NotFound();
            }
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex,ex.Message);
            return NotFound(new { message = $"User with ID {userId} not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting user with ID {UserId}.", userId);
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    ///  Geeft de userId terug van de gebruiker met de gegeven auth0UserId
    /// </summary>
    /// <param name="auth0UserId">The auth0 ID van de gebruiker.</param>
    /// <response code="200">id van de gebruiker</response>
    /// <response>403 Forbidden</response>
    /// <response code="404">Not found.</response>
    /// <response code="500">Unexpected error occurred.</response>
    [Authorize(Policy = "OwnDataOrAdmin")]
    [HttpGet("users/id/{auth0UserId}")]
    public async Task<ActionResult<int>> GetUserIdAsync(string auth0UserId)
    {   
        _logger.LogInformation("GET request received for retrieving user ID with auth0UserId \"{Auth0UserId}\".", auth0UserId);
        try
        {
            int? userId = await _userService.GetUserIdAsync(auth0UserId);
            if (userId == 0)
            {
                _logger.LogWarning("User with auth0UserId \"{Auth0UserId}\" not found.", auth0UserId);
                return NotFound(
                    new { message = $"User with auth0 user id {auth0UserId} not found." }
                );
            }
            else
            {
                _logger.LogInformation("User with auth0UserId \"{Auth0UserId}\" found.", auth0UserId);
                return Ok(userId);
            }
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
                "An unexpected error occurred while retrieving user ID with auth0UserId \"{Auth0UserId}\".",
                auth0UserId
            );
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }
}
