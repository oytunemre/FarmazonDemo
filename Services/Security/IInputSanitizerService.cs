namespace FarmazonDemo.Services.Security
{
    public interface IInputSanitizerService
    {
        string SanitizeHtml(string input);
        string SanitizeForSql(string input);
        string StripAllTags(string input);
        bool ContainsMaliciousContent(string input);
        string SanitizeFileName(string fileName);
    }
}
