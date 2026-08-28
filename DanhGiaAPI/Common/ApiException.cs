using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Common
{
    // Exception dùng chung cho mọi Service — controller/middleware bắt và map
    // sang HTTP status code tương ứng, tránh lặp try/catch ở từng action.
    public class ApiException : Exception
    {
        public int StatusCode { get; }

        public ApiException(string message, int statusCode = StatusCodes.Status400BadRequest) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
