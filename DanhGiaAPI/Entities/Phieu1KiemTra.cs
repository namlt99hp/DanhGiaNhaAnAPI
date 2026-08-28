namespace DanhGiaAPI.Entities
{
    public class Phieu1KiemTra
    {
        public int Id { get; set; }
        public string SoHieu { get; set; } = null!;
        public DateTime NgayKiemTra { get; set; }
        public int BepAnId { get; set; }
        public int NhaThauId { get; set; }
        public int PhongBanId { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP"; // NHAP, CHO_KY, DA_DUYET, TU_CHOI
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
