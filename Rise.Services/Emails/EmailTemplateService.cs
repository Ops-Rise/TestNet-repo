// Rise.Services/Emails/EmailTemplateService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rise.Shared.Emails;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly string _templatesPath;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(IConfiguration configuration, ILogger<EmailTemplateService> logger)
    {
        _logger = logger;
        try
        {
            _templatesPath =
                configuration["EmailTemplates:Path"]
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Emails");

            if (!Directory.Exists(_templatesPath))
            {
                throw new DirectoryNotFoundException(
                    $"Email templates directory not found at: {_templatesPath}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing EmailTemplateService");
            throw;
        }
    }

    public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
    {
        try
        {
            var templatePath = Path.Combine(_templatesPath, $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Email template '{templateName}' not found at: {templatePath}"
                );
            }

            var template = await File.ReadAllTextAsync(templatePath);

            foreach (var prop in typeof(T).GetProperties())
            {
                var value = prop.GetValue(model)?.ToString() ?? "";
                if (prop.PropertyType == typeof(DateTime))
                {
                    value = ((DateTime)prop.GetValue(model)).ToString("dd/MM/yyyy HH:mm");
                }
                template = template.Replace($"{{{prop.Name}}}", value);
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering email template {TemplateName}", templateName);
            throw;
        }
    }
}
