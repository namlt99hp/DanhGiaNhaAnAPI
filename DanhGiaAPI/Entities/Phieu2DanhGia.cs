using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu2_DanhGia")]
    public class Phieu2DanhGia
    {
        public int Id { get; set; }
        public string SoHieu { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int NhaThauId { get; set; }
        public int? BepAnId { get; set; }
        // Danh sách Nhà ăn của phiếu này nằm ở bảng liên kết Phieu2_NhaAn (N-N,
        // xem Entities/Phieu2NhaAn.cs) — 1 phiếu có thể gộp nhiều nhà ăn, KHÔNG
        // còn là 1 cột scalar ở đây.
        public DateTime? ThoiGianTu { get; set; }
        public DateTime? ThoiGianDen { get; set; }
        public string? DiaDiem { get; set; }
        // Nhập tự do (VD: "Từ 16h30 đến 19h00") — hiển thị ở cột "Thời gian kiểm tra" của Phiếu 2 in ra.
        // Tách biệt với ThoiGianTu/ThoiGianDen (DateTime, giữ lại để suy ra "Ngày kiểm tra" ở đầu phiếu).
        public string? ThoiGianKiemTraText { get; set; }
        // NULL nếu ngày kiểm tra không sinh Phiếu 1 tương ứng — điểm VSATTP để
        // trống, nhập tay sau (xem Phieu2_DanhGiaSuatAn.md).
        public int? Phieu1Id { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP";
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
