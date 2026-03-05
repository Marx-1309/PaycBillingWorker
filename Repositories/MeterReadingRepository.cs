using Microsoft.Data.SqlClient;
using System.Data;

namespace PaycBillingWorker.Repositories
{
    public class MeterReadingRepository
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public MeterReadingRepository(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }

        public async Task<List<string>> GetCustomerSerialNumbersAsync()
        {
            var serialNumbers = new List<string>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("aa_BS_GetCityTapCustomersSerialNumbers", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            serialNumbers.Add(reader["MeterSerialNumber"].ToString());
                        }
                    }
                }
            }

            return serialNumbers;
        }

        public async Task UpdateCustomerReadingAsync(int reading, string serialNumber)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("aa_BS_UpdateCityTapsWaterReadings", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CurrentReading", reading);
                    cmd.Parameters.AddWithValue("@CustomerSerialNo", serialNumber);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
