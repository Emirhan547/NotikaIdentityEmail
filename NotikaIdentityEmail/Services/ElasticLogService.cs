using NotikaIdentityEmail.Models.Elastics;
using System.Net.Http.Json;

namespace NotikaIdentityEmail.Services
{
    public class ElasticLogService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ElasticLogService> _logger;

        public ElasticLogService(HttpClient httpClient, IConfiguration configuration, ILogger<ElasticLogService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Validate configuration
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new InvalidOperationException("Elastic:BaseUrl yapılandırması eksik");
            }
            if (string.IsNullOrWhiteSpace(IndexPattern))
            {
                throw new InvalidOperationException("Elastic:IndexPattern yapılandırması eksik");
            }
        }

        private string BaseUrl => _configuration["Elastic:BaseUrl"]!;
        private string IndexPattern => _configuration["Elastic:IndexPattern"]!;

        public async Task<List<ElasticLogItemDto>> GetLatestAsync(int size = 20)
        {
            try
            {
                var url = $"{BaseUrl}/{IndexPattern}/_search";

                var body = new
                {
                    size,
                    sort = new object[]
                    {
                        new Dictionary<string, object>
                        {
                            ["@timestamp"] = new { order = "desc" }
                        }
                    }
                };

                var resp = await _httpClient.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var data = await resp.Content.ReadFromJsonAsync<ElasticSearchResponse>();
                if (data == null) return new List<ElasticLogItemDto>();

                return data.Hits.Hits.Select(h => Map(h.Source)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch üzerinden son loglar alınamadı");
                return new List<ElasticLogItemDto>();
            }
        }

        public async Task<int> GetErrorCountLast24hAsync()
        {
            try
            {
                var url = $"{BaseUrl}/{IndexPattern}/_count";

                var body = new
                {
                    query = new
                    {
                        @bool = new
                        {
                            filter = new object[]
                            {
                                new { terms = new { level = new [] { "Error", "Fatal" } } },
                                new { range = new Dictionary<string, object>
                                    {
                                        ["@timestamp"] = new { gte = "now-24h" }
                                    }
                                }
                            }
                        }
                    }
                };

                var resp = await _httpClient.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var data = await resp.Content.ReadFromJsonAsync<ElasticCountResponse>();
                return data?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch üzerinden hata sayısı alınamadı");
                return 0;
            }
        }

        public async Task<List<ElasticLogItemDto>> GetLatestErrorsAsync(int size = 5)
        {
            try
            {
                var url = $"{BaseUrl}/{IndexPattern}/_search";

                var body = new
                {
                    size,
                    query = new
                    {
                        terms = new
                        {
                            level = new[] { "Error", "Fatal" }
                        }
                    },
                    sort = new object[]
                    {
                        new Dictionary<string, object>
                        {
                            ["@timestamp"] = new { order = "desc" }
                        }
                    }
                };

                var resp = await _httpClient.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var data = await resp.Content.ReadFromJsonAsync<ElasticSearchResponse>();
                if (data == null) return new List<ElasticLogItemDto>();

                return data.Hits.Hits.Select(h => Map(h.Source)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Elasticsearch üzerinden son hatalar alınamadı");
                return new List<ElasticLogItemDto>();
            }
        }

        public async Task<ElasticLogItemDto?> GetByIdAsync(string id)
        {
            try
            {
                var url = $"{BaseUrl}/{IndexPattern}/_doc/{id}";

                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                if (json == null || !json.TryGetValue("_source", out var srcObj)) return null;

                // ✅ DÜZELTME: Doğru deserializasyon
                var sourceJson = System.Text.Json.JsonSerializer.Serialize(srcObj);
                var src = System.Text.Json.JsonSerializer.Deserialize<ElasticSource>(sourceJson);

                return src == null ? null : Map(src);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{LogId} kimlikli log alınamadı", id);
                return null;
            }
        }

        private static ElasticLogItemDto Map(ElasticSource src)
        {
            var dto = new ElasticLogItemDto
            {
                Timestamp = src.Timestamp,
                Level = src.Level,
                MessageTemplate = src.MessageTemplate,
                RenderedMessage = src.RenderedMessage ?? src.Message
            };

            if (src.Fields == null) return dto;

            dto.RequestPath = GetString(src.Fields, "RequestPath");
            dto.StatusCode = GetInt(src.Fields, "StatusCode");
            dto.Elapsed = GetDouble(src.Fields, "Elapsed");

            dto.UserEmail = GetString(src.Fields, "UserEmail");
            dto.MessageId = GetInt(src.Fields, "MessageId");
            dto.CategoryId = GetInt(src.Fields, "CategoryId");
            dto.IsDraft = GetBool(src.Fields, "IsDraft");
            dto.IsRead = GetBool(src.Fields, "IsRead");

            dto.ExceptionMessage = GetString(src.Fields, "ExceptionMessage");

            return dto;
        }

        private static string? GetString(Dictionary<string, object> fields, string key)
            => fields.TryGetValue(key, out var v) ? v?.ToString() : null;

        private static int? GetInt(Dictionary<string, object> fields, string key)
        {
            if (!fields.TryGetValue(key, out var v) || v == null) return null;
            return int.TryParse(v.ToString(), out var i) ? i : null;
        }

        private static double? GetDouble(Dictionary<string, object> fields, string key)
        {
            if (!fields.TryGetValue(key, out var v) || v == null) return null;
            return double.TryParse(v.ToString(), out var d) ? d : null;
        }

        private static bool? GetBool(Dictionary<string, object> fields, string key)
        {
            if (!fields.TryGetValue(key, out var v) || v == null) return null;
            return bool.TryParse(v.ToString(), out var b) ? b : null;
        }
    }
}