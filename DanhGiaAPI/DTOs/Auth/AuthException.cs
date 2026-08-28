using DanhGiaAPI.Common;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.DTOs.Auth
{
    public class AuthException : ApiException
    {
        public AuthException(string message, int statusCode = StatusCodes.Status400BadRequest) : base(message, statusCode)
        {
        }
    }
}
