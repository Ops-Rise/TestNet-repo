using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.ProfileImages;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Rise.Shared.Users;

namespace Rise.Server.Controllers;

[ApiController]
[Route("api/")]
public class ProfileImageController(
    IProfileImageService profileImageService,
    IUserService userService,
    ILogger<ProfileImageController> logger
) : ControllerBase
{
    private readonly IProfileImageService _profileImageService = profileImageService;
    private readonly IUserService _userService = userService;
    private readonly ILogger<ProfileImageController> _logger = logger;

    private const string UnexpectedErrorMessage =
        "An unexpected error occurred while processing your request.";

    /// <summary>
    /// Retrieves the profile image for a user with the given userId
    /// </summary>
    /// <param name="userId">The userId of the user.</param>
    /// <response>200 and image (file)</response>
    /// <response>404 Not Found</response>
    /// <response>403 Forbidden</response>
    [HttpGet("profileimage/{userId}")]
    public async Task<IActionResult> GetProfileImageAsync(int userId)
    {

        try
        {
            if (!await IsUserAuthorized(userId))
            {
                _logger.LogWarning("User is not authorized to view this profile image.");
                return Forbid();
            }

            var profileImage = await _profileImageService.GetProfileImageAsync(userId);
            
            if (profileImage == null)
            {
                _logger.LogWarning("Profile image not found for userId {UserId}.", userId);
                return NotFound(new { message = "Profile image not found" });
            }

            _logger.LogInformation("Profile image retrieved successfully for userId {UserId}.", userId);
            return File(profileImage.ImageBlob, profileImage.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving profile image for userId {UserId}.", userId);
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Creates a profile image for a user.
    /// </summary>
    /// <param name="userId">The userId of the user.</param>
    /// <param name="profileImageDto">The request containing the image (blob and content type).</param>
    /// <response>200</response>
    /// <response>400 BadRequest</response>
    /// <response>403 Forbidden</response>
    /// <response>500 Internal Server Error</response>
    [HttpPost("profileimage/{userId}")]
    public async Task<IActionResult> CreateProfileImageAsync(int userId, [FromBody] ProfileImageDto.Mutate profileImageDto)
    {
        _logger.LogInformation("POST request received for creating a profile image for userId {UserId}.", userId);

        try
        {
            if (!await IsUserAuthorized(userId))
            {
                _logger.LogWarning("User is not authorized to create a profile image.");
                return Forbid();
            }
            if (profileImageDto.ImageBlob == null || profileImageDto.ImageBlob.Length == 0)
            {
                _logger.LogWarning("Profile image content is empty for userId {UserId}.", userId);
                return BadRequest(new { message = "Profile image content cannot be empty." });
            }

            if (string.IsNullOrEmpty(profileImageDto.ContentType))
            {
                _logger.LogWarning("Profile image content type is missing for userId {UserId}.", userId);
                return BadRequest(new { message = "Profile image content type is required." });
            }

            var mutateProfileImageDto = new ProfileImageDto.Mutate
            {
                ImageBlob = profileImageDto.ImageBlob,
                ContentType = profileImageDto.ContentType
            };

            await _profileImageService.CreateProfileImageAsync(userId, mutateProfileImageDto);

            _logger.LogInformation("Profile image created successfully for userId {UserId}.", userId);
            return Ok(new { message = "Profile image created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating profile image for userId {UserId}.", userId);
            return StatusCode(500, new { message = UnexpectedErrorMessage });
        }
    }

    /// <summary>
    /// Updates an existing profile image for a user.
    /// </summary>
    /// <param name="userId">The userId of the user.</param>
    /// <param name="profileImageDto">The request containing the image (blob and content type).</param>
    /// <response>200</response>
    /// <response>400 BadRequest</response>
    /// <response>403 Forbidden</response>
    /// <response>500 Internal Server Error</response>
    [HttpPut("profileimage/{userId}")]
    public async Task<IActionResult> UpdateProfileImageAsync(int userId, [FromBody] ProfileImageDto.Edit profileImageDto)
    {
        _logger.LogInformation("PUT request received for updating a profile image for userId {UserId}.", userId);

        try
        {
            if (!await IsUserAuthorized(userId))
            {
                _logger.LogWarning("User is not authorized to update a profile image.");
                return Forbid();
            }
            if (profileImageDto.ImageBlob == null || profileImageDto.ImageBlob.Length == 0)
            {
                _logger.LogWarning("Profile image content is empty for userId {UserId}.", userId);
                return BadRequest(new { message = "Profile image content cannot be empty." });
            }

            if (string.IsNullOrEmpty(profileImageDto.ContentType))
            {
                _logger.LogWarning("Profile image content type is missing for userId {UserId}.", userId);
                return BadRequest(new { message = "Profile image content type is required." });
            }

            await _profileImageService.UpdateProfileImageAsync(userId, profileImageDto);

            _logger.LogInformation("Profile image updated successfully for userId {UserId}.", userId);
            return Ok(new { message = "Profile image updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating profile image for userId {UserId}.", userId);
            return StatusCode(500, new { message = UnexpectedErrorMessage });
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
            //als de user iets van andere users opvraagt  => Forbid
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