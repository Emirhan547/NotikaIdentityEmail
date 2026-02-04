using System.Text.Json.Serialization;

namespace NotikaIdentityEmail.Models.Elastics
{
    public class ElasticSearchResponse
    {
        [JsonPropertyName("hits")]
        public ElasticHits Hits { get; set; } = new();
    }

    public class ElasticHits
    {
        [JsonPropertyName("hits")]
        public List<ElasticHit> Hits { get; set; } = new();
    }
}

