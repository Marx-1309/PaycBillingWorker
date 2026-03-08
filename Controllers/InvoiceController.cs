using Microsoft.AspNetCore.Mvc;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Models;
using PaycBillingWorker.Services;

namespace PaycBillingWorker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpPost("PostInvoice")]
        public async Task<IActionResult> PostInvoice([FromBody] InvoicePayload payload)
        {
            var response = await _invoiceService.PostInvoiceToApi(payload);

            if (response.IsSuccess)
                return StatusCode(201, response.Result);

            return BadRequest(new { message = response.Message });
        }
    }
}
