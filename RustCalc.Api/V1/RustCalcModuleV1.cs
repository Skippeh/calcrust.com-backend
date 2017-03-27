using System;
using System.Text;
using Nancy;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RustCalc.Api.V1
{
    public abstract class RustCalcModuleV1 : RustCalcModule
    {
        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        protected RustCalcModuleV1(string modulePath) : base("/v1" + modulePath)
        {
            
        }

        protected Func<object, object> WrapMethod(Func<object, object> func)
        {
            return parameters =>
            {
                object apiResponse;

                try
                {
                    apiResponse = func(parameters);
                }
                catch (ApiResponseException ex)
                {
                    apiResponse = new ApiResponseV1(null, ex.StatusCode, ex.Message);
                }
                catch (Exception ex)
                {
                    apiResponse = new ApiResponseV1(null, HttpStatusCode.InternalServerError, ex.ToString());
                }

                Response response = new Response
                {
                    StatusCode = (apiResponse as ApiResponseV1)?.StatusCode ?? HttpStatusCode.OK,
                    Contents = stream =>
                    {
                        var bytes = Encoding.UTF8.GetBytes(apiResponse is string ? (string)apiResponse : JsonConvert.SerializeObject(apiResponse, serializerSettings));
                        stream.Write(bytes, 0, bytes.Length);
                    },
                    ContentType = "application/json"
                };
                return response;
            };
        }
    }
}