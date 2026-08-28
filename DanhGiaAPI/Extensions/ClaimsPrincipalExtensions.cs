using System.Security.Claims;

namespace DanhGiaAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetNguoiDungId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("sub")?.Value;
            return value != null ? int.Parse(value) : 0;
        }
    }
}
