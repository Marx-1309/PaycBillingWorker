using PaycBillingWorker.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaycBillingWorker.Interfaces
{
    public interface IMeterReadingService
    {
        Task<ResponseDTO> GetReadingsBySerialAsync(string serialNumber, int page, int pageSize);
    }
}
