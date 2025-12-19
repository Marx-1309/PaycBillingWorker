using PaycBillingWorker.Models.DTO;

namespace PaycBillingWorker.Interfaces
{
    public interface IBaseService
    {
        Task<ResponseDTO?> SendAsync(RequestDTO requestDto, bool withBearer = true);
    }
}
