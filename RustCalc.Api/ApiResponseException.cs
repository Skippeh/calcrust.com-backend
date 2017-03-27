using System;
using Nancy;

namespace RustCalc.Api
{
    public class ApiResponseException : Exception
    {
        public HttpStatusCode StatusCode;

        public ApiResponseException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}