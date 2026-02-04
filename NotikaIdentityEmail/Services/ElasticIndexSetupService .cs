// Services/ElasticIndexSetupService.cs
namespace NotikaIdentityEmail.Services
{
    public class ElasticIndexSetupService : IHostedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ElasticIndexSetupService> _logger;

        public ElasticIndexSetupService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ElasticIndexSetupService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var baseUrl = _configuration["Elastic:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("Elasticsearch BaseUrl not configured, skipping index template setup");
                return;
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var templateUrl = $"{baseUrl}/_index_template/notika-logs-template";

                var template = new
                {
                    index_patterns = new[] { "notika-logs-*" },
                    template = new
                    {
                        settings = new
                        {
                            number_of_shards = 1,
                            number_of_replicas = 0,
                            refresh_interval = "5s"
                        },
                        mappings = new
                        {
                            properties = new
                            {
                                timestamp = new { type = "date" },
                                level = new { type = "keyword" },
                                messageTemplate = new { type = "text" },
                                renderedMessage = new { type = "text" },
                                fields = new
                                {
                                    properties = new
                                    {
                                        UserEmail = new { type = "keyword" },
                                        RequestPath = new { type = "keyword" },
                                        StatusCode = new { type = "integer" },
                                        Elapsed = new { type = "double" },
                                        MessageId = new { type = "integer" },
                                        CategoryId = new { type = "integer" }
                                    }
                                }
                            }
                        }
                    }
                };

                var response = await httpClient.PutAsJsonAsync(templateUrl, template, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Elasticsearch index template created successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to create Elasticsearch template. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Elasticsearch template (non-critical)");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}