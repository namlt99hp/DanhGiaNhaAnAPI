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

        // NULL nếu tài khoản không phải nhà thầu (nội bộ) — dùng để lọc dữ
        // liệu Phiếu 1-4 theo đúng nhà thầu của người gọi (xem Phieu*Service).
        public static int? GetNhaThauId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("nha_thau_id")?.Value;
            return value != null ? int.Parse(value) : null;
        }

        // NULL nếu tài khoản không thuộc phòng ban nào (nhà thầu, hoặc admin
        // chưa gán phòng ban) — dùng để lọc dữ liệu theo đúng phòng ban của
        // người gọi (VD Phieu2Service.DanhSachPhieu1KhaDungAsync).
        public static int? GetPhongBanId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("phong_ban_id")?.Value;
            return value != null ? int.Parse(value) : null;
        }

        // Claim "admin"="1" — set khi VaiTro.LaQuanTriVien = 1 (xem TaoToken
        // trong AuthService). Dùng để bypass các ràng buộc quyền theo phiếu
        // (NguoiDungPhieuQuyen) giống hệt cách CoQuyen() ở Program.cs bypass
        // policy quyền chung — admin luôn được phép mọi thao tác.
        public static bool GetLaAdmin(this ClaimsPrincipal user) =>
            user.HasClaim("admin", "1");
    }
}
