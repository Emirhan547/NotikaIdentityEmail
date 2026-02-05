using System.Text.Json.Serialization;

namespace NotikaIdentityEmail.Models.Elastics
{
    public class ElasticSource
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
        public Dictionary<string, object>? Fields { get; set; }

    
        [JsonPropertyName("exceptions")]
        public object? Exceptions { get; set; }
    }
}
