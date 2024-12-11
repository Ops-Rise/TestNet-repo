namespace Rise.Shared.Emails;

public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync<T>(string templateName, T model);
}
