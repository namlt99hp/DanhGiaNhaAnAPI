namespace DanhGiaAPI.Entities
{
    public class Phieu1KiemTra
    {
        public int Id { get; set; }
        // NULL tới khi phiếu hoàn tất ký duyệt (DA_DUYET) — xem Phieu1Service.DongBoTrangThaiAsync
        public string? SoHieu { get; set; }
        public DateTime NgayKiemTra { get; set; }
        public int BepAnId { get; set; }
        public int NhaThauId { get; set; }
        public int PhongBanId { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP"; // NHAP, CHO_KY, DA_DUYET, TU_CHOI
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Số lần phiếu này đã bị thu hồi (CHO_KY/DA_DUYET -> NHAP để sửa lại)
        // — tăng mỗi lần Phieu1Service.ThuHoiAsync chạy. Chi tiết từng lần
        // (ai, khi nào, lý do) nằm ở Phieu1_ThuHoi, xem Entities/Phieu1ThuHoi.cs.
        public int SoLanThuHoi { get; set; } = 0;
    }
}
