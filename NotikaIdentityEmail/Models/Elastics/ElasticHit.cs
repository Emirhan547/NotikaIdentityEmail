using System.Text.Json.Serialization;

namespace NotikaIdentityEmail.Models.Elastics
{
    public class ElasticHit
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("_source")]
        public ElasticSource Source { get; set; } = new();
    }
}
