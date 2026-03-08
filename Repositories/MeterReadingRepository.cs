using Microsoft.Data.SqlClient;
using System.Data;

namespace PaycBillingWorker.Repositories
{
    public class MeterReadingRepository
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly ILogger<MeterReadingRepository> _logger;

        public MeterReadingRepository(IConfiguration config, ILogger<MeterReadingRepository> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }

        public async Task<List<string>> GetCustomerSerialNumbersAsync()
        {
            var serialNumbers = new List<string>();

            try
            {
                _logger.LogInformation("Starting GetCustomerSerialNumbersAsync.");

                using SqlConnection conn = new SqlConnection(_connectionString);
                using SqlCommand cmd = new SqlCommand("aa_BS_GetCityTapCustomersSerialNumbers", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await conn.OpenAsync();
                _logger.LogInformation("Database connection opened successfully.");

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var serialNumber = reader["MeterSerialNumber"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(serialNumber))
                    {
                        serialNumbers.Add(serialNumber);
                    }
                }

                _logger.LogInformation("Fetched {Count} customer serial numbers.", serialNumbers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred in GetCustomerSerialNumbersAsync. Message: {ErrorMessage}",
                    ex.Message);
                throw;
            }

            return serialNumbers;
        }

        public async Task UpdateCustomerReadingAsync(int reading, string serialNumber)
        {
            try
            {
                _logger.LogInformation("Updating meter reading for SerialNumber: {SerialNumber} with Reading: {Reading}",
                    serialNumber, reading);

                using SqlConnection conn = new SqlConnection(_connectionString);
                using SqlCommand cmd = new SqlCommand("aa_BS_UpdateCityTapsWaterReadings", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@CurrentReading", reading);
                cmd.Parameters.AddWithValue("@CustomerSerialNo", serialNumber);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Successfully updated meter reading for SerialNumber: {SerialNumber}", serialNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while updating meter reading for SerialNumber: {SerialNumber}. Message: {ErrorMessage}",
                    serialNumber, ex.Message);
                throw;
            }
        }
    }
}