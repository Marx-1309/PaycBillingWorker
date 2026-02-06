using Microsoft.AspNetCore.Mvc;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models.DTO;
using PaycBillingWorker.Services;

namespace PaycBillingWorker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeterReadingController : ControllerBase
    {
        private readonly IMeterReadingService _meterReadingService;

        public MeterReadingController(IMeterReadingService meterReadingService)
        {
            _meterReadingService = meterReadingService;
        }

        [HttpGet("GetBySerialNumber/{serialNumber}")]
        public async Task<IActionResult> GetBySerialNumber(string serialNumber, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(serialNumber))
            {
                return BadRequest(new { message = "Serial Number is required" });
            }

            var response = await _meterReadingService.GetReadingsBySerialAsync(serialNumber, page, pageSize);

            if (response != null && response.IsSuccess)
            {
                return Ok(response.Result);
            }

            return BadRequest(new { message = response?.Message });
        }
    }
}