using Microsoft.AspNetCore.Mvc;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models;
using PaycBillingWorker.Models.DTO;
using PaycBillingWorker.Services;

namespace PaycBillingWorker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsumerController : ControllerBase
    {
        private readonly IConsumerService _consumerService;

        public ConsumerController(IConsumerService consumerService)
        {
            _consumerService = consumerService;
        }

        [HttpPost("PostNewConsumer")]
        public async Task<IActionResult> PostNewConsumer([FromBody] ConsumerPayload payload)
        {
            if (string.IsNullOrEmpty(payload.Name) ||
                string.IsNullOrEmpty(payload.Email) ||
                string.IsNullOrEmpty(payload.Phone) ||
                string.IsNullOrEmpty(payload.AltContact))
            {
                return BadRequest(new { message = "Name, Email, Phone, and AltContact are required." });
            }

            var response = await _consumerService.PostNewConsumerAsync(payload);

            if (response.IsSuccess)
                return StatusCode(201, response.Result);

            return BadRequest(new { message = response.Message });
        }

        [HttpPut("UpdateConsumer")]
        public async Task<IActionResult> UpdateConsumer([FromBody] ConsumerPayload payload)
        {
            var response = await _consumerService.UpdateConsumerAsync(payload);

            if (response != null && response.IsSuccess)
            {
                return Ok(response);
            }
            return StatusCode(400, response);
        }
    }
}