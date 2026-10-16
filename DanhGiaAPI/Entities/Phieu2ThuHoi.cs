namespace DanhGiaAPI.Entities
{
    // Lịch sử thu hồi Phiếu 2 — xem Phieu1ThuHoi.cs (cùng thiết kế, tách bảng
    // riêng theo đúng phiếu) + Phieu2Service.ThuHoiAsync +
    // 02. Phantich/modules/Phieu2_DanhGiaSuatAn.md.
    public class Phieu2ThuHoi
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int PhienBan { get; set; }
        public string TrangThaiTruocKhiThuHoi { get; set; } = null!;
        public int NguoiThuHoiId { get; set; } // -> NguoiDung.Id, không FK (claim "sub")
        public DateTime NgayThuHoi { get; set; } = DateTime.Now;
        public string LyDoThuHoi { get; set; } = null!;
    }
}
