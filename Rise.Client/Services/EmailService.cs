using System.Net.Http.Json;
using Rise.Shared.Emails;
using Rise.Shared.Mailer;

namespace Rise.Client.Services
{
    public class EmailService(HttpClient httpClient) : IEmailService
    {
        private readonly HttpClient _httpClient = httpClient;
        private const string Endpoint = "email";

        /// <summary>
        /// Sends an email to a specified recipient.
        /// </summary>
        /// <param name="toEmail">Recipient's email address</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="htmlContent">HTML content of the email</param>
        /// <exception cref="ArgumentException">Thrown if the request is invalid.</exception>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var requestBody = new EmailDto.Mutate
            {
                To = toEmail,
                Subject = subject,
                Body = htmlContent,
            };

            var response = await _httpClient.PostAsJsonAsync($"{Endpoint}/send", requestBody);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new ArgumentException(errorMessage);
            }

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Sends an email to multiple recipients.
        /// </summary>
        /// <param name="to">List of recipient email addresses</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="htmlContent">HTML content of the email</param>
        public async Task SendEmailAsync(List<string> to, string subject, string htmlContent)
        {
            var requestBody = new EmailDto.MutateMultiple
            {
                Tos = to,
                Subject = subject,
                Body = htmlContent,
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{Endpoint}/send-multiple",
                requestBody
            );

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new ArgumentException(errorMessage);
            }

            response.EnsureSuccessStatusCode();
        }

        public Task SendTemplatedEmailAsync(string toEmail, string subject, string templatedContent)
        {
            throw new NotImplementedException();
        }

        public Task SendTemplatedEmailAsync(
            List<string> toEmails,
            string subject,
            string templatedContent
        )
        {
            throw new NotImplementedException();
        }
    }
}
