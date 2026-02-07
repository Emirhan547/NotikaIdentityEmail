using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NotikaIdentityEmail.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotikaIdentityEmail.Services.HuggingFaces
{
    public class HuggingFaceService : IHuggingFaceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // 🔥 HF label → Türkçe karşılık
        private static readonly Dictionary<string, string> TurkishLabelMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "toxic", "Zararlı İçerik" },
                { "severe_toxic", "Ağır Zararlı İçerik" },
                { "insult", "Hakaret" },
                { "threat", "Tehdit" },
                { "obscene", "Müstehcen İçerik" },
                { "identity_hate", "Kimlik Temelli Nefret" },
                { "hate", "Nefret Söylemi" },
                { "nothate", "Zararsız İçerik" }
            };

        private readonly HttpClient _httpClient;
        private readonly HuggingFaceOptions _options;
        private readonly IMemoryCache _cache;

        public HuggingFaceService(
            HttpClient httpClient,
            IOptions<HuggingFaceOptions> options,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _cache = cache;
        }

        // =========================================================
        // 🧠 TOXICITY ANALYSIS
        // =========================================================
        public async Task<ToxicityAnalysisResult?> AnalyzeToxicityAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(_options.ApiKey))
                return null;

            var response = await SendWithRetryAsync(
                _options.ToxicityModel,
                text,
                cancellationToken);

            if (response == null)
                return null;

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            List<List<HuggingFaceLabelScore>>? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<List<List<HuggingFaceLabelScore>>>(payload, JsonOptions);
            }
            catch
            {
                return null;
            }

            var scores = parsed?.FirstOrDefault();
            if (scores == null || scores.Count == 0)
                return null;

            // 🔥 Sadece bizim tanıdığımız label’ları dikkate al
            var matchedScore = scores
                .Where(s => TurkishLabelMap.ContainsKey(s.Label))
                .OrderByDescending(s => s.Score)
                .FirstOrDefault();

            var isToxic =
                matchedScore != null &&
                matchedScore.Label != "nothate" &&
                matchedScore.Score >= _options.ToxicityThreshold;

            var turkishLabel =
                matchedScore != null && TurkishLabelMap.TryGetValue(matchedScore.Label, out var tr)
                    ? tr
                    : "Zararsız İçerik";

            return new ToxicityAnalysisResult
            {
                Label = turkishLabel,
                Score = matchedScore?.Score ?? 0,
                IsToxic = isToxic
            };
        }

        // =========================================================
        // 🔁 RETRY – HF ROUTER ENDPOINT
        // =========================================================
        private async Task<HttpResponseMessage?> SendWithRetryAsync(
            string model,
            string text,
            CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://router.huggingface.co/hf-inference/models/{model}")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { inputs = text }),
                        Encoding.UTF8,
                        "application/json")
                };

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (payload.Contains("\"estimated_time\"", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(2000, cancellationToken);
                    continue;
                }

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
            }

            return null;
        }

        // =========================================================
        // 🌍 TRANSLATION
        // =========================================================
        public async Task<string?> TranslateAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(_options.ApiKey))
                return null;

            var cacheKey = $"hf:tr:{ComputeHash(text)}";
            if (_cache.TryGetValue(cacheKey, out string? cached))
                return cached;

            var model = IsLikelyTurkish(text)
                ? _options.TranslateTrToEnModel
                : _options.TranslateEnToTrModel;

            var response = await SendRequestAsync(model, text, cancellationToken);
            if (response == null)
                return null;

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (IsModelLoading(payload))
                return null;

            var translations =
                JsonSerializer.Deserialize<List<HuggingFaceTranslationResult>>(payload, JsonOptions);

            var translation = translations?.FirstOrDefault()?.TranslationText?.Trim();
            if (string.IsNullOrWhiteSpace(translation))
                return null;

            _cache.Set(cacheKey, translation, TimeSpan.FromMinutes(30));
            return translation;
        }

        private async Task<HttpResponseMessage?> SendRequestAsync(
            string model,
            string text,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://router.huggingface.co/hf-inference/models/{model}")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { inputs = text }),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode ? response : null;
        }

        private static bool IsModelLoading(string payload) =>
            string.IsNullOrWhiteSpace(payload) ||
            payload.Contains("\"estimated_time\"", StringComparison.OrdinalIgnoreCase) ||
            payload.Contains("\"error\"", StringComparison.OrdinalIgnoreCase);

        private static bool IsLikelyTurkish(string text) =>
            text.IndexOfAny(new[] { 'ğ', 'Ğ', 'ü', 'Ü', 'ş', 'Ş', 'ö', 'Ö', 'ç', 'Ç', 'ı', 'İ' }) >= 0;

        private static string ComputeHash(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }
    }
}
