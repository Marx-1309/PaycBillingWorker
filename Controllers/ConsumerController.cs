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
            // Basic Validation based on "Required: Yes" fields in docs
            if (string.IsNullOrEmpty(payload.Name) ||
                string.IsNullOrEmpty(payload.Email) ||
                string.IsNullOrEmpty(payload.Phone) ||
                string.IsNullOrEmpty(payload.AltContact))
            {
                return BadRequest(new ResponseDTO
                {
                    IsSuccess = false,
                    Message = "Name, Email, Phone, and AltContact are required fields."
                });
            }

            var response = await _consumerService.PostNewConsumerAsync(payload);

            if (response != null && response.IsSuccess)
            {
                return StatusCode(201, response); // 201 Created
            }

            // Return 400 with the specific error message (e.g., Duplicate email)
            return StatusCode(400, response);
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