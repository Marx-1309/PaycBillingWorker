using PaycBillingWorker.Models;
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
        //private readonly ITokenProvider _tokenProvider;
        public BaseService(IHttpClientFactory httpClientFactory/*, ITokenProvider tokenProvider*/)
        {
            _httpClientFactory = httpClientFactory;
            //_tokenProvider = tokenProvider;
        }

        public async Task<ResponseDTO?> SendAsync(RequestDTO requestDto, bool withBearer = true)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient("PaycBillingWorkerAPI");
                HttpRequestMessage message = new();
                if (requestDto.ContentType == ContentType.MultipartFormData)
                {
                    message.Headers.Add("Accept", "*/*");
                }
                else
                {
                    message.Headers.Add("Accept", "application/json");
                }
                //token
                if (withBearer)
                {
                    var token = PaycBillingWorker.Utility.SD.Token;
                    message.Headers.Add("Authorization", $"Bearer {token}");
                }

                message.RequestUri = new Uri(requestDto.Url);

                if (requestDto.ContentType == ContentType.MultipartFormData)
                {
                    var content = new MultipartFormDataContent();

                    foreach (var prop in requestDto.Data.GetType().GetProperties())
                    {
                        var value = prop.GetValue(requestDto.Data);
                        if (value is FormFile)
                        {
                            var file = (FormFile)value;
                            if (file != null)
                            {
                                content.Add(new StreamContent(file.OpenReadStream()), prop.Name, file.FileName);
                            }
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
                        message.Content = new StringContent(JsonConvert.SerializeObject(requestDto.Data), Encoding.UTF8, "application/json");
                    }
                }

                HttpResponseMessage? apiResponse = null;

                if (requestDto.ApiType == ApiType.POST)
                {
                    message.Method = HttpMethod.Post;
                }
                else if (requestDto.ApiType == ApiType.DELETE)
                {
                    message.Method = HttpMethod.Delete;
                }
                else if (requestDto.ApiType == ApiType.PUT)
                {
                    message.Method = HttpMethod.Put;
                }
                else
                {
                    message.Method = HttpMethod.Get;
                }

                apiResponse = await client.SendAsync(message);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return new() { IsSuccess = false, Message = "Not Found" };
                }
                else if (apiResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new() { IsSuccess = false, Message = "Access Denied" };
                }
                else if (apiResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new() { IsSuccess = false, Message = "Unauthorized" };
                }
                else if (apiResponse.StatusCode == HttpStatusCode.InternalServerError)
                {
                    return new() { IsSuccess = false, Message = "Internal Server Error" };
                }
                else
                {

                    var apiContent = await apiResponse.Content.ReadAsStringAsync();
                    List<Object> meterReadings = JsonConvert.DeserializeObject<List<Object>>(apiContent);

                    ResponseDTO responseDto = new ResponseDTO
                    {
                        Result = meterReadings,
                        IsSuccess = true,
                        Message = "Data successfully deserialized"
                    };
                    return responseDto;
                }

            }
            catch (Exception ex)
            {
                var dto = new ResponseDTO
                {
                    Message = ex.Message.ToString(),
                    IsSuccess = false
                };
                return dto;
            }
        }
    }
}
