namespace PaycBillingWorker.Models.DTO
{
    public class ResponseDTO<T>
    {
        public T? Result { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = "";
    }

    public class MeterReadingApiResponse
    {
        public string Message { get; set; }
        public MeterReadingItem MeterReading { get; set; }
    }

    public class MeterReadingItem
    {
        public string Id { get; set; }
        public JsonMetadata JsonMetadata { get; set; }
    }

    public class JsonMetadata
    {
        public long DevEui { get; set; }
        public double Reading { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class ApiMessageResponse
    {
        public string Message { get; set; }
    }

}
