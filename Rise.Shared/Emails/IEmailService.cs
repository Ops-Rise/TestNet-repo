namespace Rise.Shared.Emails;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
    Task SendEmailAsync(List<string> to, string subject, string htmlContent);
    Task SendTemplatedEmailAsync(string toEmail, string subject, string templatedContent);
    Task SendTemplatedEmailAsync(List<string> toEmails, string subject, string templatedContent);
}
