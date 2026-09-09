namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    // Thay thế TOÀN BỘ tập bước ký được gán trực tiếp cho 1 tài khoản — cùng
    // kiểu với CapNhatVaiTroRequest (gửi thiếu 1 Id = mất quyền ký bước đó).
    public class CapNhatLuongKyRequest
    {
        public List<int> MauLuongKyIds { get; set; } = new();
    }
}
