using PaycBillingWorker.Models.DTO;
using PaycBillingWorker.Interfaces;

namespace PaycBillingWorker.Services
{
    public class MeterReadingService : IMeterReadingService
    {
        private readonly IBaseService _baseService;
        private readonly IConfiguration _config;

        public MeterReadingService(IBaseService baseService, IConfiguration config)
        {
            _baseService = baseService;
            _config = config;
        }

        public async Task<ResponseDTO> GetReadingsBySerialAsync(string serialNumber, int page, int pageSize)
        {
            // 1. Get Base URL (Use the one from your prompt if not in appsettings)
            // Defaulting to the one provided in your prompt if config is missing
            var baseUrl = _config["ApiSettings:BaseUrl"] ?? "https://server.watermeter.payc.online";

            // 2. Construct Endpoint path
            // Documentation Endpoint: /api/v1/mqttmeterReading/ByMeterSerialNumber/:id
            var endpoint = $"/api/v1/mqttmeterReading/ByMeterSerialNumber/{serialNumber}";

            // 3. Add Query Parameters (page, pageSize)
            var fullUrl = $"{baseUrl}{endpoint}?page={page}&pageSize={pageSize}";

            // 4. Send Request via BaseService
            return await _baseService.SendAsync(new RequestDTO
            {
                Url = fullUrl,
                Data = null, // GET request has no body
                ApiType = Utility.SD.ApiType.GET,
                ContentType = Utility.SD.ContentType.Json
            });
        }
    }
}