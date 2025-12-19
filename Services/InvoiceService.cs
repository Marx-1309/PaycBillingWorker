using System.Data;
using Microsoft.Data.SqlClient; 
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PaycBillingWorker.Models;
using PaycBillingWorker.Models.DTO;

namespace PaycBillingWorker.Services
{
    public interface IInvoiceService
    {
        Task ProcessInvoicesAsync();
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ILogger<InvoiceService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly BaseService _baseService;

        public InvoiceService(ILogger<InvoiceService> logger, IConfiguration config, HttpClient httpClient, BaseService baseService)
        {
            _logger = logger;
            _config = config;
            _httpClient = httpClient;

            var baseUrl = _config["ApiSettings:BaseUrl"];
            var token = _config["ApiSettings:Token"];

            if (!string.IsNullOrEmpty(baseUrl))
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            _baseService = baseService;
        }

        public async Task ProcessInvoicesAsync()
        {
            _logger.LogInformation("Starting Invoice Processing...");
            var rawRows = new List<InvoiceSourceRow>();

            string connectionString = _config.GetConnectionString("DefaultConnection");

            // 1. Fetch data using SqlDataReader (No Dapper)
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Note: Column names in Reader match the View aliases (e.g., 'reference_key', 'invoice_no')
                    string sql = "SELECT TOP 50 * FROM [dbo].[aaKWSS_CITY_TAP_API] ORDER BY invoice_no";

                    using (var command = new SqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        command.CommandTimeout = 60000000;
                        while (await reader.ReadAsync())
                        {
                            var row = new InvoiceSourceRow();

                            // Safe parsing with DBNull checks
                            row.ReferenceKey = reader["reference_key"] != DBNull.Value ? reader["reference_key"].ToString() : null;
                            row.ReferenceType = reader["reference_type"] != DBNull.Value ? reader["reference_type"].ToString() : "account_number";
                            row.InvoiceNo = reader["invoice_no"] != DBNull.Value ? reader["invoice_no"].ToString() : null;

                            row.InvoiceDate = reader["invoice_date"] != DBNull.Value ? Convert.ToDateTime(reader["invoice_date"]) : DateTime.MinValue;
                            row.InvoiceFromDate = reader["invoice_from_date"] != DBNull.Value ? Convert.ToDateTime(reader["invoice_from_date"]) : DateTime.MinValue;
                            row.InvoiceToDate = reader["invoice_to_date"] != DBNull.Value ? Convert.ToDateTime(reader["invoice_to_date"]) : DateTime.MinValue;

                            // Handle decimals
                            row.OpeningReading = reader["opening_reading"] != DBNull.Value ? Convert.ToDecimal(reader["opening_reading"]) : (decimal?)null;
                            row.ClosingReading = reader["closing_reading"] != DBNull.Value ? Convert.ToDecimal(reader["closing_reading"]) : (decimal?)null;
                            row.OpeningBalance = reader["opening_balance"] != DBNull.Value ? Convert.ToDecimal(reader["opening_balance"]) : 0m;
                            row.ThisPeriodCharges = reader["this_period_charges"] != DBNull.Value ? Convert.ToDecimal(reader["this_period_charges"]) : 0m;
                            row.TotalPayable = reader["total_payable"] != DBNull.Value ? Convert.ToDecimal(reader["total_payable"]) : 0m;
                            row.MinimumPayable = reader["minimum_payable"] != DBNull.Value ? Convert.ToDecimal(reader["minimum_payable"]) : 0m;

                            row.PayByDate = reader["pay_by_date"] != DBNull.Value ? Convert.ToDateTime(reader["pay_by_date"]) : DateTime.MinValue;
                            row.Comment = reader["comment"] != DBNull.Value ? reader["comment"].ToString() : "";
                            row.SpecialComment = reader["special_comment"] != DBNull.Value ? reader["special_comment"].ToString() : "";

                            // Line Items
                            row.SerialNo = reader["serial_no"] != DBNull.Value ? reader["serial_no"].ToString() : "";
                            row.Description = reader["description"] != DBNull.Value ? reader["description"].ToString() : "";
                            row.UnitRate = reader["unit_rate"] != DBNull.Value ? Convert.ToDecimal(reader["unit_rate"]) : 0m;
                            row.Quantity = reader["quantity"] != DBNull.Value ? Convert.ToDecimal(reader["quantity"]) : 0m;
                            row.Nett = reader["nett"] != DBNull.Value ? Convert.ToDecimal(reader["nett"]) : 0m;
                            row.Vat = reader["vat"] != DBNull.Value ? Convert.ToDecimal(reader["vat"]) : 0m;
                            row.Amount = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0m;

                            rawRows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database or read data.");
                return;
            }

            if (!rawRows.Any())
            {
                _logger.LogInformation("No pending invoices found in view.");
                return;
            }

            // 2. Group Data
            var invoiceGroups = rawRows.GroupBy(r => r.InvoiceNo);

            foreach (var group in invoiceGroups)
            {
                try
                {
                    var header = group.First();

                    var payload = new InvoicePayload
                    {
                        ReferenceKey = header.ReferenceKey,
                        ReferenceType = header.ReferenceType,
                        InvoiceNo = header.InvoiceNo,
                        InvoiceDate = header.InvoiceDate.ToString("yyyy-MM-dd"),
                        InvoiceFromDate = header.InvoiceFromDate.ToString("yyyy-MM-dd"),
                        InvoiceToDate = header.InvoiceToDate.ToString("yyyy-MM-dd"),
                        OpeningReading = header.OpeningReading,
                        ClosingReading = header.ClosingReading,
                        OpeningBalance = header.OpeningBalance,
                        ThisPeriodCharges = header.ThisPeriodCharges,
                        TotalPayable = header.TotalPayable,
                        MinimumPayable = header.MinimumPayable,
                        PayByDate = header.PayByDate.ToString("yyyy-MM-dd"),
                        Comment = header.Comment,
                        SpecialComment = header.SpecialComment,

                        LineItems = group.Select(g => new InvoiceLineItem
                        {
                            SerialNo = g.SerialNo,
                            Description = g.Description,
                            UnitRate = g.UnitRate,
                            Quantity = g.Quantity,
                            Nett = g.Nett,
                            Vat = g.Vat,
                            Amount = g.Amount
                        }).ToList()
                    };

                    await PostInvoiceToApi(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing invoice group: {group.Key}");
                }
            }
        }

        private async Task PostInvoiceToApi(InvoicePayload payload)
        {
            try
            { 
                var baseUrl = _config["ApiSettings:BaseUrl"];
                var endpoint = _config["ApiSettings:InvoiceEndpoint"];
               // var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
               var response = await _baseService.SendAsync(new Models.DTO.RequestDTO
                {
                    Url = baseUrl + endpoint,
                    Data = payload,
                    ContentType = Utility.SD.ContentType.Json,
                    ApiType = Utility.SD.ApiType.POST
               });
                var apiResponse = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"EXCEPTION: Failed to post invoice {payload.InvoiceNo}");
            }
        }
    }
}