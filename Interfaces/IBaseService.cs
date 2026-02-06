using PaycBillingWorker.Models.DTO;

namespace PaycBillingWorker.Interfaces
{
    public interface IBaseService
    {
        Task<ResponseDTO<T>?> SendAsync<T>(RequestDTO requestDto, bool withBearer = true);
    }
}
