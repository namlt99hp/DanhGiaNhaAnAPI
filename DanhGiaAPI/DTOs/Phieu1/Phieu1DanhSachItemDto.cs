namespace DanhGiaAPI.DTOs.Phieu1
{
    // Dùng cho GET /api/phieu1 (danh sách) — phẳng hóa Phieu1KiemTra + kèm tên
    // người đã ký (Nhà thầu / Phòng ban) resolve sẵn từ ChuKyPhieu, để trang
    // danh sách hiển thị 2 cột đó mà không cần gọi riêng từng phiếu (xem
    // Phieu1Service.DanhSachAsync/GanTenNguoiDaKyAsync).
    public class Phieu1DanhSachItemDto
    {
        public int Id { get; set; }
        public string SoHieu { get; set; } = string.Empty;
        public DateTime NgayKiemTra { get; set; }
        public int BepAnId { get; set; }
        public int NhaThauId { get; set; }
        public int PhongBanId { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP";
        public DateTime NgayTao { get; set; }

        // Họ tên người ký ĐÃ DUYỆT (lượt ký mới nhất) — phân biệt theo tài
        // khoản ký có NhaThauId hay không, KHÔNG theo BuocThuTu (xem
        // GanTenNguoiDaKyAsync). NULL khi chưa ai ký bước tương ứng.
        public string? TenNhaThauDaKy { get; set; }
        public string? TenNguoiDaKy { get; set; }
    }
}
