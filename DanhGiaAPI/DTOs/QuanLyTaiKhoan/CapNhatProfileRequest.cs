using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    // Tự cập nhật thông tin cá nhân (khác CapNhatVaiTroRequest — vốn chỉ
    // Admin dùng). Không cho đổi TenDangNhap/PhongBanId/NhaThauId ở đây — 2
    // trường sau do Admin quản lý qua modal "Gán vai trò"/tạo tài khoản.
    public class CapNhatProfileRequest
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(255)]
        public string HoTen { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }
    }
}
