using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.Auth
{
    public class DangKyRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MaxLength(100)]
        public string TenDangNhap { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhau { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(255)]
        public string HoTen { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }
        public int? PhongBanId { get; set; }
        public int? NhaThauId { get; set; }
    }
}
