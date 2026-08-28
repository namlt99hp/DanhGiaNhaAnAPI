using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class DoiMatKhauRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        public string MatKhauHienTai { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string MatKhauMoi { get; set; } = null!;
    }
}
