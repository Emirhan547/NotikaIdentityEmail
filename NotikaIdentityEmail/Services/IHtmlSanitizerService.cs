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
            _sanitizer.AllowedTags.UnionWith(new[] { "p", "br", "strong", "b", "em", "i", "ul", "ol", "li", "blockquote", "span", "h1", "h2", "h3", "h4", "h5", "h6" });
            _sanitizer.AllowedAttributes.UnionWith(new[] { "href", "title", "target", "style" });
            _sanitizer.AllowedCssProperties.UnionWith(new[] { "color", "background-color", "font-weight", "font-size", "text-decoration" });
        }

        public string Sanitize(string html)
        {
            return _sanitizer.Sanitize(html ?? string.Empty);
        }
    }
}