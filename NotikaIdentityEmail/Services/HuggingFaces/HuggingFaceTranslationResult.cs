using System.Text.Json.Serialization;

namespace NotikaIdentityEmail.Services.HuggingFaces
{
    public class HuggingFaceTranslationResult
    {
        [JsonPropertyName("translation_text")]
        public string TranslationText { get; set; } = string.Empty;
    }
}
