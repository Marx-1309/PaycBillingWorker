using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models;
using PaycBillingWorker.Models.DTO;
using static PaycBillingWorker.Utility.SD;

namespace PaycBillingWorker.Services
{

    public class ConsumerService : IConsumerService
    {
        private readonly IBaseService _baseService;
        private readonly IConfiguration _config;

        public ConsumerService(IBaseService baseService, IConfiguration config)
        {
            _baseService = baseService;
            _config = config;
        }

        public async Task<ResponseDTO> PostNewConsumerAsync(ConsumerPayload payload)
        {
            var baseUrl = _config["ApiSettings:BaseUrl"];
            // Defined in doc Section 3: /api/v1/consumer/post_new_consumer
            var endpoint = "/api/v1/consumer/post_new_consumer";

            return await _baseService.SendAsync(new RequestDTO
            {
                Url = baseUrl + endpoint,
                Data = payload,
                ApiType = Utility.SD.ApiType.POST,
                ContentType = Utility.SD.ContentType.Json
            });
        }

        public async Task<ResponseDTO> UpdateConsumerAsync(ConsumerPayload payload)
        {
            var baseUrl = _config["ApiSettings:BaseUrl"];
            // Defined in doc Section 4: Endpoint
            var endpoint = "/api/v1/consumer/update_consumer";

            return await _baseService.SendAsync(new RequestDTO
            {
                Url = baseUrl + endpoint,
                Data = payload,
                ApiType = Utility.SD.ApiType.PUT,
                ContentType = Utility.SD.ContentType.Json
            });
        }
    }
}