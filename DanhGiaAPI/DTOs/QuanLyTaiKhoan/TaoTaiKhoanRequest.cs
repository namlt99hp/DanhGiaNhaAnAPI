using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    // Admin (có quyền duyệt tài khoản) tạo trực tiếp 1 tài khoản mới — khác
    // với DangKyRequest (tự đăng ký, luôn CHO_DUYET): tài khoản tạo ở đây vào
    // thẳng HOAT_DONG, không cần duyệt lại (xem NguoiDungService.TaoTaiKhoanAsync).
    public class TaoTaiKhoanRequest
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
        public List<int> VaiTroIds { get; set; } = new();

        // Phân quyền theo Phiếu — gán luôn lúc tạo tài khoản (tùy chọn), xem
        // 02. Phantich/modules/VaiTro.md mục 9.
        public List<int> MauLuongKyIds { get; set; } = new();
        public List<NguoiDungPhieuQuyenItemDto> PhieuQuyen { get; set; } = new();
    }
}
