using PaycBillingWorker.Utility;
using static PaycBillingWorker.Utility.SD;

namespace PaycBillingWorker.Models.DTO
{
    public class RequestDTO
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public object Data { get; set; }
        public string AccessToken { get; set; }
        public ContentType ContentType { get; set; } = ContentType.Json;
    }
}
