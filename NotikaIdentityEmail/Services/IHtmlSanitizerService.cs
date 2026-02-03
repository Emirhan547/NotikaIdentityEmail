using Ganss.Xss;

namespace NotikaIdentityEmail.Services
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string html);
    }

    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService()
        {
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.UnionWith(new[] { "p", "br", "strong", "b", "em", "i", "ul", "ol", "li", "blockquote" });
            _sanitizer.AllowedAttributes.UnionWith(new[] { "href", "title", "target" });
        }

        public string Sanitize(string html)
        {
            return _sanitizer.Sanitize(html ?? string.Empty);
        }
    }
}