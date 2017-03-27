using Nancy;
using Newtonsoft.Json;

namespace RustCalc.Api.V1
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiResponseV1
    {
        public HttpStatusCode StatusCode { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        public ApiResponseV1(object data, HttpStatusCode statusCode = HttpStatusCode.OK, string message = null)
        {
            Data = data;
            StatusCode = statusCode;
            Message = message;
        }
    }
}