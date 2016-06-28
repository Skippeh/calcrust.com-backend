using System;
using System.Text;
using Nancy;
using Newtonsoft.Json;

namespace WebAPI
{
    public enum ApiResult
    {
        Success,
        DataLoading,
        BadInput
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ApiResponse
    {
        [JsonProperty]
        public HttpStatusCode StatusCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        public ApiResponse(object data, HttpStatusCode statusCode = HttpStatusCode.OK, string message = null)
        {
            if (data is ApiResponse)
            {
                throw new ArgumentException("Data of ApiResponse can not be of type ApiResponse as well.");
            }

            Data = data;
            StatusCode = statusCode;
            Message = message;
        }
    }
}