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

        public async Task<ResponseDTO<ApiMessageResponse>> PostNewConsumerAsync(ConsumerPayload payload)
        {
            var baseUrl = _config["ApiSettings:BaseUrl"];
            var endpoint = "/api/v1/consumer/post_new_consumer";

            return await _baseService.SendAsync<ApiMessageResponse>(new RequestDTO
            {
                Url = baseUrl + endpoint,
                Data = payload,
                ApiType = ApiType.POST,
                ContentType = ContentType.Json
            });
        }

        public async Task<ResponseDTO<ApiMessageResponse>> UpdateConsumerAsync(ConsumerPayload payload)
        {
            var baseUrl = _config["ApiSettings:BaseUrl"];
            var endpoint = "/api/v1/consumer/update_consumer";

            return await _baseService.SendAsync<ApiMessageResponse>(new RequestDTO
            {
                Url = baseUrl + endpoint,
                Data = payload,
                ApiType = ApiType.PUT,
                ContentType = ContentType.Json
            });
        }
    }
}
