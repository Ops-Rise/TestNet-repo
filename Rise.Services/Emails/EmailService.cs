using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Rise.Shared.Emails;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Rise.Services.Emails
{
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _client;
        private readonly IConfiguration _config;
        private readonly IEmailTemplateService _templateService;

        public EmailService(IConfiguration config, IEmailTemplateService templateService)
        {
            _config = config;
            _templateService = templateService;
            var apiKey = _config["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("SendGrid API key is not configured.");
            }

            _client = new SendGridClient(apiKey);
        }

        private async Task SendEmailInternalAsync(
            List<EmailAddress> toEmails,
            string subject,
            string htmlContent
        )
        {
            Console.WriteLine("Sending email...");

            var from = new EmailAddress("simon.zachee@student.hogent.be", "buut"); // Replace with the actual sender email
            var plainTextContent = "This is the plain text version of the email."; // Optional fallback

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                from,
                toEmails,
                subject,
                plainTextContent,
                htmlContent
            );

            try
            {
                var response = await _client.SendEmailAsync(msg);

                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                {
                    Console.WriteLine("Email sent successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to send email. Status: {response.StatusCode}");

                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"Response body: {responseBody}");
                    throw new($"Failed to send email. Status:  {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }

        // Method to send email to a single recipient
        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var model = new { RecipientName = "Buurtbewoner", EmailContent = htmlContent };
            var processedContent = await _templateService.RenderTemplateAsync(
                "DefaultEmail",
                model
            );

            var recipients = new List<EmailAddress> { new EmailAddress(toEmail) };
            await SendEmailInternalAsync(recipients, subject, processedContent);
        }

        // Method to send email to multiple recipients
        public async Task SendEmailAsync(List<string> toEmails, string subject, string htmlContent)
        {
            var model = new { RecipientName = "Buurtbewoner", EmailContent = htmlContent };
            var processedContent = await _templateService.RenderTemplateAsync(
                "DefaultEmail",
                model
            );

            var emailAddresses = new List<EmailAddress>();
            foreach (var email in toEmails)
            {
                emailAddresses.Add(new EmailAddress(email));
            }
            await SendEmailInternalAsync(emailAddresses, subject, processedContent);
        }

        public async Task SendTemplatedEmailAsync(
            string toEmail,
            string subject,
            string templatedContent
        )
        {
            var recipients = new List<EmailAddress> { new EmailAddress(toEmail) };
            await SendEmailInternalAsync(recipients, subject, templatedContent);
        }

        public async Task SendTemplatedEmailAsync(
            List<string> toEmails,
            string subject,
            string templatedContent
        )
        {
            var emailAddresses = toEmails.Select(email => new EmailAddress(email)).ToList();
            await SendEmailInternalAsync(emailAddresses, subject, templatedContent);
        }
    }
}
