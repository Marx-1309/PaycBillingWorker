using Newtonsoft.Json;

namespace PaycBillingWorker.Models
{
    public class MeterReadingResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        // Maps "meter Reading" from the documentation explicitly
        [JsonProperty("meter Reading")]
        public List<MeterReadingItem> MeterReadings { get; set; }
    }

    public class MeterReadingItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("jsonMetadata")]
        public MeterMetadata JsonMetadata { get; set; }
    }

    public class MeterMetadata
    {
        [JsonProperty("devEui")]
        public string DevEui { get; set; }

        [JsonProperty("reading")]
        public decimal Reading { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}