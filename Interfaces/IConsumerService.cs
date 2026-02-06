using PaycBillingWorker.Models.DTO;
using PaycBillingWorker.Models;

namespace PaycBillingWorker.Interfaces
{
    public interface IConsumerService
    {
        Task<ResponseDTO<ApiMessageResponse>> PostNewConsumerAsync(ConsumerPayload payload);
        Task<ResponseDTO<ApiMessageResponse>> UpdateConsumerAsync(ConsumerPayload payload);
    }
}
