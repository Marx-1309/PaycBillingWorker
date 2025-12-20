using System.Data;
using Microsoft.Data.SqlClient;
using System.Net.Http.Headers;
using PaycBillingWorker.Models;
using PaycBillingWorker.Models.DTO;
using PaycBillingWorker.Interfaces; // Required for IBaseService

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

        // FIX: Change type from BaseService to IBaseService
        private readonly IBaseService _baseService;

        public InvoiceService(
            ILogger<InvoiceService> logger,
            IConfiguration config,
            HttpClient httpClient,
            IBaseService baseService) // FIX: Inject interface here
        {
            _logger = logger;
            _config = config;
            _httpClient = httpClient;

            var baseUrl = _config["ApiSettings:BaseUrl"];
            var token = _config["ApiSettings:Token"];

            if (!string.IsNullOrEmpty(baseUrl))
                _httpClient.BaseAddress = new Uri(baseUrl);

            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            _baseService = baseService;
        }

        public async Task ProcessInvoicesAsync()
        {
            _logger.LogInformation("Starting Invoice Processing...");
            var rawRows = new List<InvoiceSourceRow>();

            string connectionString = _config.GetConnectionString("DefaultConnection");

            #region 1. Read Data
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string sql = "SELECT TOP 50 * FROM [dbo].[aaKWSS_CITY_TAP_API] ORDER BY invoice_no";

                using var command = new SqlCommand(sql, connection)
                {
                    CommandTimeout = 6000
                };

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new InvoiceSourceRow
                    {
                        ReferenceKey = reader["reference_key"]?.ToString(),
                        ReferenceType = reader["reference_type"]?.ToString(),
                        InvoiceNo = reader["invoice_no"]?.ToString(),

                        InvoiceDate = reader["invoice_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["invoice_date"])
                            : DateTime.MinValue,

                        InvoiceFromDate = reader["invoice_from_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["invoice_from_date"])
                            : DateTime.MinValue,

                        InvoiceToDate = reader["invoice_to_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["invoice_to_date"])
                            : DateTime.MinValue,

                        OpeningReading = reader["opening_reading"] != DBNull.Value
                            ? Convert.ToDecimal(reader["opening_reading"])
                            : null,

                        ClosingReading = reader["closing_reading"] != DBNull.Value
                            ? Convert.ToDecimal(reader["closing_reading"])
                            : null,

                        OpeningBalance = reader["opening_balance"] != DBNull.Value
                            ? Convert.ToDecimal(reader["opening_balance"])
                            : 0m,

                        ThisPeriodCharges = reader["this_period_charges"] != DBNull.Value
                            ? Convert.ToDecimal(reader["this_period_charges"])
                            : 0m,

                        TotalPayable = reader["total_payable"] != DBNull.Value
                            ? Convert.ToDecimal(reader["total_payable"])
                            : 0m,

                        MinimumPayable = reader["minimum_payable"] != DBNull.Value
                            ? Convert.ToDecimal(reader["minimum_payable"])
                            : 0m,

                        PayByDate = reader["pay_by_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["pay_by_date"])
                            : DateTime.MinValue,

                        Comment = reader["comment"]?.ToString() ?? "",
                        SpecialComment = reader["special_comment"]?.ToString() ?? "",

                        SerialNo = reader["serial_no"]?.ToString() ?? "",
                        Description = reader["description"]?.ToString() ?? "",

                        UnitRate = reader["unit_rate"] != DBNull.Value
                            ? Convert.ToDecimal(reader["unit_rate"])
                            : 0m,

                        Quantity = reader["quantity"] != DBNull.Value
                            ? Convert.ToDecimal(reader["quantity"])
                            : 0m,

                        Nett = reader["nett"] != DBNull.Value
                            ? Convert.ToDecimal(reader["nett"])
                            : 0m,

                        Vat = reader["vat"] != DBNull.Value
                            ? Convert.ToDecimal(reader["vat"])
                            : 0m,

                        Amount = reader["amount"] != DBNull.Value
                            ? Convert.ToDecimal(reader["amount"])
                            : 0m
                    };

                    rawRows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database or read data.");
                return;
            }
            #endregion

            if (!rawRows.Any())
            {
                _logger.LogInformation("No pending invoices found.");
                return;
            }

            #region 2. Group + Send
            var invoiceGroups = rawRows.GroupBy(r => r.InvoiceNo);

            foreach (var group in invoiceGroups)
            {
                try
                {
                    var header = group.First();
                    string normalizedRefType = NormalizeReferenceType(header.ReferenceType);

                    var payload = new InvoicePayload
                    {
                        ReferenceKey = header.ReferenceKey,
                        ReferenceType = normalizedRefType,
                        InvoiceNo = header.InvoiceNo,

                        InvoiceDate = ToIsoDate(header.InvoiceDate),
                        InvoiceFromDate = ToIsoDate(header.InvoiceFromDate),
                        InvoiceToDate = ToIsoDate(header.InvoiceToDate),
                        PayByDate = ToIsoDate(header.PayByDate),

                        OpeningReading = header.OpeningReading.HasValue
                            ? Round2(header.OpeningReading.Value)
                            : null,

                        ClosingReading = header.ClosingReading.HasValue
                            ? Round2(header.ClosingReading.Value)
                            : null,

                        OpeningBalance = Round2(header.OpeningBalance),
                        ThisPeriodCharges = Round2(header.ThisPeriodCharges),
                        TotalPayable = Round2(header.TotalPayable),
                        MinimumPayable = Round2(header.MinimumPayable),

                        Comment = header.Comment,
                        SpecialComment = header.SpecialComment,

                        LineItems = group.Select(g => new InvoiceLineItem
                        {
                            SerialNo = g.SerialNo,
                            Description = g.Description,

                            UnitRate = Round2(g.UnitRate),
                            Quantity = decimal.Truncate(g.Quantity),
                            Nett = Round2(g.Nett),
                            Vat = Round2(g.Vat),
                            Amount = Round2(g.Amount)
                        }).ToList()
                    };

                    await PostInvoiceToApi(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing invoice group: {group.Key}");
                }
            }
            #endregion
        }

        private async Task PostInvoiceToApi(InvoicePayload payload)
        {
            try
            {
                var baseUrl = _config["ApiSettings:BaseUrl"];
                var endpoint = _config["ApiSettings:InvoiceEndpoint"];

                var response = await _baseService.SendAsync(new RequestDTO
                {
                    Url = baseUrl + endpoint,
                    Data = payload,
                    ContentType = Utility.SD.ContentType.Json,
                    ApiType = Utility.SD.ApiType.POST
                });

                if (response != null && !response.IsSuccess)
                {
                    _logger.LogError($"API Error for Invoice {payload.InvoiceNo}: {response.Message}");
                }
                else
                {
                    _logger.LogInformation($"Successfully posted Invoice {payload.InvoiceNo}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to post invoice {payload.InvoiceNo}");
            }
        }

        #region Helpers

        private static decimal Round2(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string ToIsoDate(DateTime date)
        {
            return date == DateTime.MinValue
                ? null
                : date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        private string NormalizeReferenceType(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "account_number";

            string lower = input.ToLower().Trim();

            if (lower == "meter_serial" || lower == "account_number" || lower == "erf_number")
                return lower;

            _logger.LogWarning($"Invalid reference_type '{input}'. Defaulting to 'account_number'.");
            return "account_number";
        }

        #endregion
    }
}