namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class NguoiDungListItemDto
    {
        public int Id { get; set; }
        public string TenDangNhap { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public int? PhongBanId { get; set; }
        public int? NhaThauId { get; set; }
        public string TrangThai { get; set; } = null!;
        public int? NguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime NgayTao { get; set; }
        public List<string> DanhSachVaiTro { get; set; } = new();

        // Chỉ populate đầy đủ ở ChiTietAsync (dùng cho form sửa) — DanhSachAsync
        // (màn danh sách) để rỗng, tránh N+1 không cần thiết.
        public List<int> DanhSachMauLuongKyId { get; set; } = new();
        public List<NguoiDungPhieuQuyenItemDto> PhieuQuyen { get; set; } = new();
    }
}
