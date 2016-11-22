using System.Net;

namespace DiscordBot.Rust
{
    public class ApiResponse<T>
    {
        public T Data { get; private set; }
        
        public HttpStatusCode StatusCode { get; internal set; }
        public string Message { get; private set; }

        public bool IsError { get; internal set; }

        public class ErrorResponse
        {
            public string Message { get; private set; }
            public WebResponse Response { get; private set; }

            public ErrorResponse(WebResponse response)
            {
                Response = response;
            }
        }

        public static ApiResponse<T> Error(HttpStatusCode httpStatusCode, string message = "")
        {
            return new ApiResponse<T>()
            {
                StatusCode = httpStatusCode,
                Message = message,
                IsError = true
            };
        }
    }
}