using Microsoft.AspNetCore.Mvc;
using PaycBillingWorker.Models;
using System.Diagnostics.Metrics;

namespace PaycBillingWorker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeterReadingController : ControllerBase
    {
        /// <summary>
        /// Retrieves meter readings for a specific meter.
        /// </summary>
        /// <remarks>
        /// Supports both direct meter serial number lookup and reference-based lookup (account_number, erf_number).
        /// </remarks>
        /// <param name="id">The Meter Serial Number or Identifier</param>
        /// <param name="query">Pagination and optional reference types</param>
        /// <returns>A list of meter readings</returns>
        [HttpGet("ByMeterSerialNumber/{id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MeterReadingResponse), 200)] // Success [Source 71]
        [ProducesResponseType(400)] // Bad Request [Source 86]
        [ProducesResponseType(401)] // Unauthorized
        [ProducesResponseType(404)] // Not Found [Source 94]
        public IActionResult GetMeterReadings(string id, [FromQuery] MeterReadingQuery query)
        {
            // 1. Validation: Check Reference Type if provided [Source 86]
            if (!string.IsNullOrEmpty(query.ReferenceType))
            {
                var validTypes = new[] { "meter_serial", "account_number", "erf_number" };
                if (Array.IndexOf(validTypes, query.ReferenceType) == -1)
                {
                    return BadRequest(new { message = "Invalid reference_type. Must be one of: meter_serial, account_number, erf_number" });
                }
            }

            // 2. Mock Logic (Replace with actual SQL DB Call)
            // Simulating "Not Found" for a specific ID
            if (id == "UNKNOWN_METER")
            {
                return NotFound(new { message = $"No meter found for account number: {id}" }); // [Source 97]
            }

            // 3. Construct Success Response [Source 71]
            var response = new MeterReadingResponse
            {
                Message = "Data fetched successfully",
                MeterReadings = new List<MeterReadingItem>
                {
                    new MeterReadingItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        JsonMetadata = new MeterMetadata
                        {
                            DevEui = id, // Returning the requested ID as the device EUI
                            Reading = 150.5m, // Example from [Source 82]
                            Timestamp = DateTime.UtcNow
                        }
                    }
                }
            };

            return Ok(response);
        }
    }
}
