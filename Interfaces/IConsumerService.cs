using PaycBillingWorker.Models;
using PaycBillingWorker.Models.DTO;

namespace PaycBillingWorker.Services
{
    public interface IConsumerService
    {
        Task<ResponseDTO> PostNewConsumerAsync(ConsumerPayload payload);
        Task<ResponseDTO> UpdateConsumerAsync(ConsumerPayload payload);
    }
}