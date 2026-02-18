using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models.DTO;
using static PaycBillingWorker.Utility.SD;

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

        public async Task<ResponseDTO<MeterReadingApiResponse>> GetReadingsBySerialAsync(
            string serialNumber, int page, int pageSize)
        {
            var baseUrl = _config["ApiSettings:BaseUrl"] ?? "https://server.watermeter.payc.online";

            var endpoint = $"/api/v1/mqttmeterReading/ByMeterSerialNumber/{serialNumber}";

            // FIXED casing → pageSize
            var fullUrl = $"{baseUrl}{endpoint}?page={page}&pageSize={pageSize}";

            return await _baseService.SendAsync<MeterReadingApiResponse>(new RequestDTO
            {
                Url = fullUrl,
                ApiType = ApiType.GET
            });
        }
    }
}


//https://server.watermeter.payc.online/api/v1/mqttmeterReading/ByMeterSerialNumber/14503898?page=1&pagesize=10
//https://server.watermeter.payc.online/api/v1/mqttmeterReading/ByMeterSerialNumber/14503898?page=1&pageSize=10