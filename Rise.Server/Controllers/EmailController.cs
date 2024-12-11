using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rise.Shared.Emails;
using Rise.Shared.Mailer;
using Rise.Shared.Users;

namespace Rise.Server.Controllers
{
    [Authorize(Roles = "Administrator")]
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController(IEmailService emailService, ILogger<EmailController> logger)
        : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<EmailController> _logger = logger;
        private const string UnexpectedErrorMessage =
            "An unexpected error occurred while sending the email.";

        /// <summary>
        /// Sends an email to a specified recipient.
        /// </summary>
        /// <param name="emailDto">Een emailDTO met de aanvullende emailgegevens voor de email service.</param>
        /// <response code="200">Email sent successfully.</response>
        /// <response code="400">Invalid email details provided.</response>
        /// <response code="500">Unexpected error occurred while sending the email.</response>
        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailDto.Mutate emailDto)
        {
            _logger.LogInformation("POST request received to send email to {ToEmail}", emailDto.To);

            try
            {
                await _emailService.SendEmailAsync(emailDto.To, emailDto.Subject, emailDto.Body);
                _logger.LogInformation("Email sent successfully to {ToEmail}", emailDto.To);
                return Ok(new { Message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while sending email to {ToEmail}",
                    emailDto.To
                );
                return StatusCode(500, new { Message = UnexpectedErrorMessage });
            }
        }

        /// <summary>
        /// Sends an email to multiple recipients.
        /// </summary>
        /// <param name="emailDto">An EmailDTO with the necessary email details for multiple recipients.</param>
        /// <response code="200">Email sent successfully to all recipients.</response>
        /// <response code="400">Invalid email details provided.</response>
        /// <response code="500">Unexpected error occurred while sending the email.</response>
        [HttpPost("send-multiple")]
        public async Task<IActionResult> SendEmailToMultiple(
            [FromBody] EmailDto.MutateMultiple emailDto
        )
        {
            _logger.LogInformation("POST request received to send email to multiple recipients.");

            try
            {
                await _emailService.SendEmailAsync(emailDto.Tos, emailDto.Subject, emailDto.Body);
                _logger.LogInformation(
                    "Email sent successfully to {RecipientCount} recipients",
                    emailDto.Tos.Count
                );
                return Ok(new { Message = "Emails sent successfully to all recipients." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending email to multiple recipients.");
                return StatusCode(500, new { Message = UnexpectedErrorMessage });
            }
        }
    }
}
