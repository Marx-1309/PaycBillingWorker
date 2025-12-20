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

        public async Task<ResponseDTO?> SendAsync(RequestDTO requestDto, bool withBearer = true)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient("PaycBillingWorkerAPI");
                HttpRequestMessage message = new();

                // 1. Set Accept Headers
                if (requestDto.ContentType == ContentType.MultipartFormData)
                {
                    message.Headers.Add("Accept", "*/*");
                }
                else
                {
                    message.Headers.Add("Accept", "application/json");
                }

                // 2. Add Authorization
                if (withBearer)
                {
                    var token = PaycBillingWorker.Utility.SD.Token;
                    message.Headers.Add("Authorization", $"Bearer {token}");
                }

                message.RequestUri = new Uri(requestDto.Url);

                // 3. Serialize Payload (Data)
                if (requestDto.ContentType == ContentType.MultipartFormData)
                {
                    var content = new MultipartFormDataContent();
                    foreach (var prop in requestDto.Data.GetType().GetProperties())
                    {
                        var value = prop.GetValue(requestDto.Data);
                        if (value is Microsoft.AspNetCore.Http.IFormFile file && file != null)
                        {
                            content.Add(new StreamContent(file.OpenReadStream()), prop.Name, file.FileName);
                        }
                        else
                        {
                            content.Add(new StringContent(value == null ? "" : value.ToString()), prop.Name);
                        }
                    }
                    message.Content = content;
                }
                else
                {
                    if (requestDto.Data != null)
                    {
                        // JSON Serialization
                        message.Content = new StringContent(
                            JsonConvert.SerializeObject(requestDto.Data),
                            Encoding.UTF8,
                            "application/json");
                    }
                }

                // 4. Set Method
                switch (requestDto.ApiType)
                {
                    case ApiType.POST: message.Method = HttpMethod.Post; break;
                    case ApiType.DELETE: message.Method = HttpMethod.Delete; break;
                    case ApiType.PUT: message.Method = HttpMethod.Put; break;
                    default: message.Method = HttpMethod.Get; break;
                }

                // 5. Send Request
                var apiResponse = await client.SendAsync(message);

                // 6. Handle Errors (Specifically 400 Bad Request for validation messages)
                if (!apiResponse.IsSuccessStatusCode)
                {
                    var errorContent = await apiResponse.Content.ReadAsStringAsync();
                    var failResponse = new ResponseDTO { IsSuccess = false };

                    switch (apiResponse.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            // Capture the specific validation error from the API
                            failResponse.Message = $"Bad Request: {errorContent}";
                            break;
                        case HttpStatusCode.NotFound:
                            failResponse.Message = "Not Found";
                            break;
                        case HttpStatusCode.Forbidden:
                            failResponse.Message = "Access Denied";
                            break;
                        case HttpStatusCode.Unauthorized:
                            failResponse.Message = "Unauthorized";
                            break;
                        case HttpStatusCode.InternalServerError:
                            failResponse.Message = $"Internal Server Error: {errorContent}";
                            break;
                        default:
                            failResponse.Message = $"Error {apiResponse.StatusCode}: {errorContent}";
                            break;
                    }
                    return failResponse;
                }

                // 7. Success Block
                var apiContent = await apiResponse.Content.ReadAsStringAsync();

                // Deserialize as generic object to handle different response types
                var resultData = JsonConvert.DeserializeObject<object>(apiContent);

                return new ResponseDTO
                {
                    Result = resultData,
                    IsSuccess = true,
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                return new ResponseDTO
                {
                    Message = $"Exception: {ex.Message}",
                    IsSuccess = false
                };
            }
        }
    }
}