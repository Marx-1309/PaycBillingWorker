using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaycBillingWorker.Models
{
    public class ConsumerPayload
    {
        [JsonIgnore]
        public string customerId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("altContact")]
        public string AltContact { get; set; }

        // Optional parameters (Nullable)

        [JsonProperty("ConsumerNo")]
        public string? ConsumerNo { get; set; }

        [JsonProperty("AccountNo")]
        public string? AccountNo { get; set; }

        [JsonProperty("ErfNo")]
        public string? ErfNo { get; set; }

        [JsonProperty("MeterSerialNumber")]
        public string? MeterSerialNumber { get; set; }

        [JsonProperty("openingMeterReading")]
        public decimal? OpeningMeterReading { get; set; }
    }

    public class ConsumerResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("customerId")]
        public string CustomerId { get; set; }

        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}