using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NotikaIdentityEmail.Logging
{
    public sealed class ElasticLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ElasticLoggerOptions _options;
        private IExternalScopeProvider? _scopeProvider;
        private readonly ConcurrentDictionary<string, ElasticLogger> _loggers = new();

        public ElasticLoggerProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _options = ElasticLoggerOptions.FromConfiguration(configuration);
        }

        public ILogger CreateLogger(string categoryName)
            => _loggers.GetOrAdd(categoryName, name => new ElasticLogger(name, _httpClientFactory, _options, () => _scopeProvider));

        public void Dispose()
        {
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        private sealed class ElasticLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly IHttpClientFactory _httpClientFactory;
            private readonly ElasticLoggerOptions _options;
            private readonly Func<IExternalScopeProvider?> _scopeProviderAccessor;

            public ElasticLogger(
                string categoryName,
                IHttpClientFactory httpClientFactory,
                ElasticLoggerOptions options,
                Func<IExternalScopeProvider?> scopeProviderAccessor)
            {
                _categoryName = categoryName;
                _httpClientFactory = httpClientFactory;
                _options = options;
                _scopeProviderAccessor = scopeProviderAccessor;
            }

            public IDisposable BeginScope<TState>(TState state)
                => _scopeProviderAccessor()?.Push(state) ?? NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel)
                => logLevel >= _options.MinimumLevel && !_options.ShouldExclude(_categoryName) && _options.Enabled;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var renderedMessage = formatter(state, exception);
                if (string.IsNullOrWhiteSpace(renderedMessage))
                {
                    return;
                }

                var fields = new Dictionary<string, object?>
                {
                    ["Category"] = _categoryName
                };

                if (eventId.Id != 0)
                {
                    fields["EventId"] = eventId.Id;
                }

                if (!string.IsNullOrWhiteSpace(eventId.Name))
                {
                    fields["EventName"] = eventId.Name;
                }

                AddStateFields(state, fields, out var messageTemplate);
                AddScopeFields(fields);

                if (exception != null)
                {
                    fields["ExceptionMessage"] = exception.Message;
                    fields["ExceptionType"] = exception.GetType().FullName;
                }

                var logEntry = new ElasticLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = logLevel.ToString(),
                    MessageTemplate = messageTemplate ?? renderedMessage,
                    Message = renderedMessage,
                    RenderedMessage = renderedMessage,
                    Fields = fields,
                    Exceptions = exception == null
                        ? null
                        : new
                        {
                            message = exception.Message,
                            stackTrace = exception.StackTrace
                        }
                };

                _ = SendAsync(logEntry);
            }

            private void AddScopeFields(Dictionary<string, object?> fields)
            {
                var scopeProvider = _scopeProviderAccessor();
                if (scopeProvider == null)
                {
                    return;
                }

                scopeProvider.ForEachScope((scope, state) =>
                {
                    AddScopeState(scope, state);
                }, fields);
            }

            private static void AddScopeState(object? scope, Dictionary<string, object?> fields)
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                {
                    foreach (var kvp in kvps)
                    {
                        if (kvp.Value != null)
                        {
                            fields[kvp.Key] = kvp.Value;
                        }
                    }

                    return;
                }

                if (scope != null)
                {
                    fields["Scope"] = scope.ToString();
                }
            }

            private static void AddStateFields<TState>(
                TState state,
                Dictionary<string, object?> fields,
                out string? messageTemplate)
            {
                messageTemplate = null;

                if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
                {
                    foreach (var kvp in kvps)
                    {
                        if (kvp.Key == "{OriginalFormat}")
                        {
                            messageTemplate = kvp.Value?.ToString();
                            continue;
                        }

                        if (kvp.Value != null)
                        {
                            fields[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            private async Task SendAsync(ElasticLogEntry entry)
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    var url = $"{_options.BaseUrl}/{_options.GetIndexName()}/_doc";
                    await httpClient.PostAsJsonAsync(url, entry);
                }
                catch
                {
                    // Loglama sırasında hata oluşursa uygulama akışını bozma.
                }
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }

        private sealed class ElasticLoggerOptions
        {
            public string BaseUrl { get; }
            public string IndexPattern { get; }
            public LogLevel MinimumLevel { get; }
            public bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl);

            private ElasticLoggerOptions(string baseUrl, string indexPattern, LogLevel minimumLevel)
            {
                BaseUrl = baseUrl.TrimEnd('/');
                IndexPattern = indexPattern;
                MinimumLevel = minimumLevel;
            }

            public static ElasticLoggerOptions FromConfiguration(IConfiguration configuration)
            {
                var baseUrl = configuration["Elastic:BaseUrl"] ?? string.Empty;
                var indexPattern = configuration["Elastic:IndexPattern"] ?? "notika-logs-*";
                var minimumLevelText = configuration["Logging:LogLevel:Default"] ?? "Information";

                if (!Enum.TryParse(minimumLevelText, true, out LogLevel minimumLevel))
                {
                    minimumLevel = LogLevel.Information;
                }

                return new ElasticLoggerOptions(baseUrl, indexPattern, minimumLevel);
            }

            public bool ShouldExclude(string categoryName)
            {
                if (categoryName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (categoryName.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            public string GetIndexName()
            {
                if (string.IsNullOrWhiteSpace(IndexPattern))
                {
                    return $"notika-logs-{DateTime.UtcNow:yyyy.MM.dd}";
                }

                if (IndexPattern.Contains('*', StringComparison.Ordinal))
                {
                    return IndexPattern.Replace("*", DateTime.UtcNow.ToString("yyyy.MM.dd"), StringComparison.Ordinal);
                }

                return IndexPattern;
            }
        }

        private sealed class ElasticLogEntry
        {
            [JsonPropertyName("@timestamp")]
            public DateTime Timestamp { get; set; }

            [JsonPropertyName("level")]
            public string? Level { get; set; }

            [JsonPropertyName("messageTemplate")]
            public string? MessageTemplate { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("renderedMessage")]
            public string? RenderedMessage { get; set; }

            [JsonPropertyName("fields")]
            public Dictionary<string, object?>? Fields { get; set; }

            [JsonPropertyName("exceptions")]
            public object? Exceptions { get; set; }
        }
    }
}