using PaycBillingWorker.Models.DTO;
using Newtonsoft.Json;
using PaycBillingWorker.Interfaces;
using System.Net;
using System.Text;
using static PaycBillingWorker.Utility.SD;

namespace PaycBillingWorker.Services
{
    public class BaseService : IBaseService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BaseService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ResponseDTO<T>?> SendAsync<T>(RequestDTO requestDto, bool withBearer = true)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient("PaycBillingWorkerAPI");
                HttpRequestMessage message = new();

                message.Headers.Add("Accept", "application/json");

                if (withBearer)
                {
                    var token = Utility.SD.Token;
                    message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                message.RequestUri = new Uri(requestDto.Url);

                if (requestDto.Data != null)
                {
                    message.Content = new StringContent(
                        JsonConvert.SerializeObject(requestDto.Data),
                        Encoding.UTF8,
                        "application/json");
                }

                message.Method = requestDto.ApiType switch
                {
                    ApiType.POST => HttpMethod.Post,
                    ApiType.DELETE => HttpMethod.Delete,
                    ApiType.PUT => HttpMethod.Put,
                    _ => HttpMethod.Get
                };

                var apiResponse = await client.SendAsync(message);

                var content = await apiResponse.Content.ReadAsStringAsync();

                if (!apiResponse.IsSuccessStatusCode)
                {
                    return new ResponseDTO<T>
                    {
                        IsSuccess = false,
                        Message = $"Error {apiResponse.StatusCode}: {content}"
                    };
                }

                var result = JsonConvert.DeserializeObject<T>(content);

                return new ResponseDTO<T>
                {
                    Result = result,
                    IsSuccess = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                return new ResponseDTO<T>
                {
                    IsSuccess = false,
                    Message = $"Exception: {ex.Message}"
                };
            }
        }

    }
}