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

        // Populate ở CẢ DanhSachAsync (theo batch, không N+1 — modal "Phân
        // quyền theo Phiếu" ở QuanLyTaiKhoanPageV2.tsx dùng thẳng row của
        // bảng danh sách, không gọi lại ChiTietAsync) lẫn ChiTietAsync.
        public List<int> DanhSachMauLuongKyId { get; set; } = new();
        public List<NguoiDungPhieuQuyenItemDto> PhieuQuyen { get; set; } = new();

        // "Trần" cấu trúc — loại phiếu tài khoản này ĐƯỢC PHÉP cấu hình Đánh
        // giá/Quản lý tiêu chí/Ký (không phải quyền thật đang có). Dùng để
        // modal "Phân quyền theo Phiếu" quyết định hiện Card của loại phiếu
        // nào. Xem NguoiDungService.TinhLoaiPhieuApDung.
        public List<string> LoaiPhieuApDung { get; set; } = new();

        // Quyền THẬT đang có — hợp của loại phiếu có DuocDanhGia=true và loại
        // phiếu có ít nhất 1 bước ký đã gán. Chỉ populate ở ChiTietAsync (dùng
        // cho sidebar/TrangChu qua GET /api/profile — xem IQuyenXemPhieuService).
        public List<string> DanhSachLoaiPhieuDuocXem { get; set; } = new();
    }
}
