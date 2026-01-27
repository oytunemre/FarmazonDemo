using System.Text.RegularExpressions;
using System.Web;

namespace FarmazonDemo.Services.Security
{
    public class InputSanitizerService : IInputSanitizerService
    {
        // Common XSS patterns
        private static readonly string[] DangerousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"onmouseover\s*=",
            @"onfocus\s*=",
            @"onblur\s*=",
            @"onsubmit\s*=",
            @"eval\s*\(",
            @"expression\s*\(",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>",
            @"<form[^>]*>",
            @"document\.cookie",
            @"document\.write",
            @"window\.location"
        };

        // SQL injection patterns
        private static readonly string[] SqlDangerousPatterns = new[]
        {
            @"('\s*(or|and)\s*'?\d)",
            @"(--|\#|\/\*)",
            @"(union\s+select)",
            @"(drop\s+table)",
            @"(insert\s+into)",
            @"(delete\s+from)",
            @"(update\s+\w+\s+set)",
            @"(exec\s*\()",
            @"(execute\s*\()",
            @"(xp_)",
            @"(sp_)"
        };

        public string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // HTML encode the input
            var sanitized = HttpUtility.HtmlEncode(input);

            return sanitized;
        }

        public string SanitizeForSql(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Replace single quotes with two single quotes (SQL escape)
            var sanitized = input.Replace("'", "''");

            // Remove null bytes
            sanitized = sanitized.Replace("\0", "");

            return sanitized;
        }

        public string StripAllTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove all HTML tags
            var result = Regex.Replace(input, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);

            // Decode HTML entities and re-encode to prevent double encoding issues
            result = HttpUtility.HtmlDecode(result);
            result = HttpUtility.HtmlEncode(result);

            return result;
        }

        public bool ContainsMaliciousContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var lowerInput = input.ToLower();

            // Check for XSS patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (Regex.IsMatch(lowerInput, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            // Check for SQL injection patterns
            foreach (var pattern in SqlDangerousPatterns)
            {
                if (Regex.IsMatch(lowerInput, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        public string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // Remove path traversal characters
            var sanitized = fileName.Replace("..", "")
                                   .Replace("/", "")
                                   .Replace("\\", "");

            // Remove invalid file name characters
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }

            // Limit length
            if (sanitized.Length > 255)
                sanitized = sanitized.Substring(0, 255);

            return string.IsNullOrEmpty(sanitized) ? "unnamed" : sanitized;
        }
    }
}
