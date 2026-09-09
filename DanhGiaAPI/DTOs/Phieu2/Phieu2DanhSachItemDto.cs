namespace DanhGiaAPI.DTOs.Phieu2
{
    // Dùng cho GET /api/phieu2 (danh sách) — phẳng hóa Phieu2DanhGia + kèm
    // NhaAnIds (lấy từ bảng liên kết Phieu2_NhaAn, xem Phieu2Service.DanhSachAsync)
    // để trang danh sách hiển thị cột "Nhà ăn" mà không cần gọi riêng ChiTietAsync.
    public class Phieu2DanhSachItemDto
    {
        public int Id { get; set; }
        public string SoHieu { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int NhaThauId { get; set; }
        public int? BepAnId { get; set; }
        public List<int> NhaAnIds { get; set; } = new();
        public DateTime? ThoiGianTu { get; set; }
        public DateTime? ThoiGianDen { get; set; }
        public string? DiaDiem { get; set; }
        public string? ThoiGianKiemTraText { get; set; }
        public int? Phieu1Id { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP";
        public DateTime NgayTao { get; set; }
    }
}
