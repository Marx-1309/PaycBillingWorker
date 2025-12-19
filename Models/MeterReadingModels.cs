using System.Text.Json.Serialization;

namespace PaycBillingWorker.Models
{
    // Response Wrapper
    public class MeterReadingResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        // Source 74: "meter Reading" (note the space in the key)
        [JsonPropertyName("meter Reading")]
        public List<MeterReadingItem> MeterReadings { get; set; }
    }

    public class MeterReadingItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("jsonMetadata")]
        public MeterMetadata JsonMetadata { get; set; }
    }

    public class MeterMetadata
    {
        [JsonPropertyName("devEui")]
        public string DevEui { get; set; }

        [JsonPropertyName("reading")]
        public decimal Reading { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    // Input Model for Reference Lookup
    public class MeterReadingQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // These are from the "Optional Request Body" in the doc [Source 69], mapped to Query for Swagger support
        public string? ReferenceKey { get; set; }
        public string? ReferenceType { get; set; } // meter_serial, account_number, erf_number
    }
}